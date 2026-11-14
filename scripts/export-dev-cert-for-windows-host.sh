#!/usr/bin/env bash
# Copy the public ASP.NET HTTPS dev cert to the repo so a Windows browser can trust it.
# The Dev Container has no Windows certificate store and no powershell.exe.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
trust_dir="${HOME}/.aspnet/dev-certs/trust"
out_dir="${repo_root}/.local"
out="${out_dir}/aspnetcore-localhost.crt"

shopt -s nullglob
pems=("${trust_dir}"/aspnetcore-localhost-*.pem)
shopt -u nullglob

if (( ${#pems[@]} == 0 )); then
  echo "No ASP.NET dev cert in ${trust_dir}." >&2
  echo "Inside the Dev Container, run: dotnet dev-certs https --trust" >&2
  exit 1
fi

newest="${pems[0]}"
for pem in "${pems[@]}"; do
  if [[ "${pem}" -nt "${newest}" ]]; then
    newest="${pem}"
  fi
done

mkdir -p "${out_dir}"
cp -L "${newest}" "${out}"
chmod 644 "${out}"

if grep -q "PRIVATE KEY" "${out}"; then
  rm -f "${out}"
  echo "Refusing to write ${out}: file contains a private key." >&2
  exit 1
fi

fingerprint="$(openssl x509 -in "${out}" -noout -fingerprint -sha1)"

host_path=""
source="$(findmnt -n -o SOURCE -T "${repo_root}" 2>/dev/null || true)"
if [[ "${source}" =~ \[([^]]+)\] ]]; then
  host_path="${BASH_REMATCH[1]}/.local/aspnetcore-localhost.crt"
fi

echo "Wrote the public certificate (no private key):"
echo "  ${out}"
echo "  ${fingerprint}"
echo
echo "In Windows PowerShell, import it, accept the trust prompt, then quit Chrome or Edge completely and reopen https://localhost:5001:"
echo
if [[ -n "${host_path}" ]]; then
  echo "  \$crt = (wsl wslpath -w -- ${host_path}).Trim()"
  echo "  Import-Certificate -FilePath \$crt -CertStoreLocation Cert:\\CurrentUser\\Root"
  echo
  echo "That wslpath command uses the default WSL distro. If it cannot see the file, import ${out} by its Windows path instead."
else
  echo "  Import-Certificate -FilePath \"<Windows path to .local\\aspnetcore-localhost.crt>\" -CertStoreLocation Cert:\\CurrentUser\\Root"
fi
