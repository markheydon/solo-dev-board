export async function copyTextToClipboard(text) {
    await navigator.clipboard.writeText(text);
}
