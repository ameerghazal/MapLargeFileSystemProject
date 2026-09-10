const dateFormatter = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
});
export function formatBytes(bytes: number): string {
    if (!Number.isFinite(bytes) || bytes < 0) return "—";
    if (bytes === 0) return "0 B";

    // TODO: Check units.
    const units = ["B", "KB", "MB", "GB", "TB"];

    const unitIndex = Math.min(
        Math.floor(Math.log(bytes) / Math.log(1024)),
        units.length - 1
    );

    const value = bytes / (1024 ** unitIndex);

    return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ` +
        units[unitIndex];
}
export function formatDate(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "—" : dateFormatter.format(date);
}
export function formatCount(folderCount: number, fileCount: number): string {
    return `${folderCount.toLocaleString()} ${folderCount === 1 ? "folder" : "folders"} ` +
           `and ${fileCount.toLocaleString()} ${fileCount === 1 ? "file" : "files"}`;
}

