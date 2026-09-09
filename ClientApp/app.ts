interface FileSystemItem {
    name: string,
    path: string,
    isDirectory: boolean,
    size: number | null,
    lastModifiedUtc: string;
}
interface ViewSummary {
    fileCount: number,
    folderCount: number,
    totalFileSize: number;
}
interface BrowseResponse {
    path: string,
    parentPath: string | null,
    items: FileSystemItem[],
    summary: ViewSummary;
}
interface SearchResponse {
    path: string,
    query: string,
    items: FileSystemItem[],
    summary: ViewSummary,
    isTruncated: boolean;
}
function getElement<T extends Element>(selector: string): T {
    const element = document.querySelector<T>(selector);

    if (element == null) {
        throw new Error(
            `Element ${selector} was not found.`
        );
    }

    return element;
}
function getPathFromURL(): string {
    const url = new URL(window.location.href);
    return url.searchParams.get("path") ?? "";
}
function getQueryFromURL(): string {
    const url = new URL(window.location.href);
    return url.searchParams.get("query") ?? "";
}
function navigate(path: string, query?: string): void {
    const url = new URL(window.location.href);

    if (path.length === 0) {
        url.searchParams.delete("path");
    } else {
        url.searchParams.set("path", path);
    }

    if (query && query.length > 0) {
        url.searchParams.set("query", query);
    } else {
        url.searchParams.delete("query");
    }

    window.history.pushState(null, "", url);

    void loadCurrentState();
}

const fileList = getElement<HTMLTableSectionElement>("#file-list");
const folderCount = getElement<HTMLElement>("#folder-count");
const fileCount = getElement<HTMLElement>("#file-count");
const totalSize = getElement<HTMLElement>("#total-size");
const statusMessage = getElement<HTMLElement>("#status-message");
const breadcrumbs = getElement<HTMLElement>("#breadcrumbs");

const searchForm = getElement<HTMLFormElement>("#search-form");
const searchInput = getElement<HTMLInputElement>("#search-input");
const clearSearchButton = getElement<HTMLButtonElement>("#clear-search");

const uploadForm = getElement<HTMLFormElement>("#upload-form");
const uploadInput = getElement<HTMLInputElement>("#upload-input");
const uploadButton = getElement<HTMLButtonElement>("#upload-button");

async function loadDirectory(path: string = ""): Promise<void> {
    renderLoading();

    try {
        const param = new URLSearchParams({ path });
        const response = await fetch(`/api/files/browse?${param}`, {
            headers: {
                Accept: "application/json"
            }
        }
        );

        // TODO: Add create response error
        //if (!res.ok) throw await

        const result = await response.json() as BrowseResponse;

        renderDirectory(result);
    } catch (error: unknown) {
        if (error instanceof Error) {
            renderError(error.message);
        } else {
            renderError("An unexpected error has occured.")
        }
    }
}

async function searchDirectory(path: string, query: string): Promise<void> {
    renderLoading();

    try {
        const param = new URLSearchParams({ path, query });
        const response = await fetch(`/api/files/search?${param}`, {
            headers: {
                Accept: "application/json"
            }
        });

        // TODO: Add create response error
        //if (!res.ok) throw await

        const result = await response.json() as SearchResponse;

        renderSearchResults(result);
    } catch (error: unknown) {
        if (error instanceof Error) {
            renderError(error.message);
        } else {
            renderError("An unexpected error has occured.")
        }
    }
}

async function uploadFile(path: string, file: File): Promise<void> {
    const formData = new FormData();
    formData.append("file", file);

    const param = new URLSearchParams({ path });

    uploadButton.disabled = true;
    statusMessage.textContent = `Uploading ${file.name}...`;

    try {
        const response = await fetch(`/api/files/upload?${param}`, {
            method: "POST",
            body: formData
        });

        // TODO: Add some error handling to avoid bug.

        const uploadedFile = await response.json() as FileSystemItem;
        uploadInput.value = "";

        const url = new URL(window.location.href);
        url.searchParams.delete("query");
        window.history.replaceState(null, "", url);

        searchInput.value = "";

        await loadDirectory(path);

        statusMessage.textContent = `Successfully uploaded ${uploadedFile.name}.`;
    } catch (error: unknown) {
        if (error instanceof Error) {
            statusMessage.textContent =
                error.message;
        } else {
            statusMessage.textContent =
                "The upload failed.";
        }
    } finally {
        uploadButton.disabled = false;
    }
}
function loadCurrentState(): void {
    const path = getPathFromURL();
    const query = getQueryFromURL();

    searchInput.value = query;

    if (query.length > 0) {
        void searchDirectory(path, query);
    } else {
        void loadDirectory(path);
    }
}
function renderLoading(): void {
    fileList.replaceChildren();
    folderCount.textContent = "0";
    fileCount.textContent = "0";
    totalSize.textContent = "0 B";
    statusMessage.textContent = "Loading directory...";
}
function renderMessageRow(message: string): void {
    const row = document.createElement("tr");
    const cell = document.createElement("td");

    cell.colSpan = 5;
    cell.textContent = message;

    row.append(cell);
    fileList.append(row);
}
function renderError(message: string): void {
    fileList.replaceChildren();
    renderMessageRow("Unable to load the directory.");

    folderCount.textContent = "0";
    fileCount.textContent = "0";
    totalSize.textContent = "0 B";
    statusMessage.textContent = message;
}
function renderDirectory(response: BrowseResponse): void {
    renderItems(response.items);
    renderSummary(response.summary);
    renderCurrentPath(response.path);

    const localFolderCount = response.summary.folderCount
    const localFileCount = response.summary.fileCount

    statusMessage.textContent =
        `Showing ${localFolderCount} ${localFolderCount === 1 ? "folder" : "folders"} ` +
        `and ${localFileCount} ${localFileCount === 1 ? "file" : "files"}.`;
}
function renderSearchResults(response: SearchResponse): void {
    renderItems(response.items);
    renderSummary(response.summary);
    renderCurrentPath(response.path);

    const localFolderCount = response.summary.folderCount
    const localFileCount = response.summary.fileCount

    const truncatedMessage = response.isTruncated ? "Only the first 500 results are shown." : "";

    statusMessage.textContent =
        `Found ${localFolderCount} ${localFolderCount === 1 ? "folder" : "folders"} ` +
        `and ${localFileCount} ${localFileCount === 1 ? "file" : "files"} ` +
        `matching "${response.query}". ${truncatedMessage}`;
}
function renderItems(items: FileSystemItem[]): void {
    fileList.replaceChildren();

    if (items.length === 0) {
        renderMessageRow("This directory is empty.");
        return;
    }

    for (const item of items) {
        const row = document.createElement("tr")

        row.append(
            createNameCell(item),
            createTextCell(item.isDirectory ? "Folder" : "File"),
            createTextCell(item.size === null ? "-" : formatBytes(item.size)),
            createTextCell(formatDate(item.lastModifiedUtc)),
            createActionCell(item)
        );

        fileList.append(row);
    }
}
function createNameCell(item: FileSystemItem): HTMLTableCellElement {
    const cell = document.createElement("td");

    const container = document.createElement("span");
    container.className = "file-name";

    const icon = document.createElement("span");
    icon.className = "icon";
    icon.textContent = item.isDirectory ? "📁" : "📄";
    icon.setAttribute("aria-hidden", "true");

    let name: HTMLElement;

    // Make the name clickable.
    if (item.isDirectory) {
        const folderBtn = document.createElement("button");

        folderBtn.type = "button"
        folderBtn.className = "folder-name";
        folderBtn.textContent = item.name;
        folderBtn.addEventListener("click", () => navigate(item.path));

        name = folderBtn;
    } else {
        const fileName = document.createElement("span");
        fileName.textContent = item.name;
        name = fileName;
    }

    container.append(icon, name);
    cell.append(container);

    return cell;
}
function createTextCell(value: string): HTMLTableCellElement {
    const cell = document.createElement("td");
    cell.textContent = value;
    return cell;
}
function createActionCell(item: FileSystemItem): HTMLTableCellElement {
    const cell = document.createElement("td");

    if (item.isDirectory) {
        cell.textContent = "-";
        return cell;
    }

    const param = new URLSearchParams({ path: item.path });

    const link = document.createElement("a");
    link.href = `/api/files/download?${param}`;
    link.className = "download-link";
    link.textContent = "Download";

    cell.append(link);
    return cell;
}
function renderSummary(summary: ViewSummary): void {
    folderCount.textContent = summary.folderCount.toLocaleString();
    fileCount.textContent = summary.fileCount.toLocaleString();
    totalSize.textContent = formatBytes(summary.totalFileSize);
}
function renderCurrentPath(path: string): void {
    breadcrumbs.replaceChildren();

    const homeButton = createBreadcrumbsButton("Home", "")

    breadcrumbs.append(homeButton);

    const segments = path.split("/").filter(segment => segment.length > 0);

    let craftedPath = "";

    for (const segment of segments) {
        craftedPath = craftedPath.length === 0 ? segment : `${craftedPath}/${segment}`;
        const targetPath = craftedPath;

        const separator = document.createElement("span");
        separator.textContent = "/";
        separator.className = "breadcrumbs-separator";

        const segementButton = createBreadcrumbsButton(segment, targetPath);

        breadcrumbs.append(separator, segementButton);
    }
}
function createBreadcrumbsButton(label: string, path: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "breadcrumb-button";
    button.textContent = label;
    button.addEventListener("click", () => navigate(path));
    return button
}
function formatBytes(bytes: number): string {
    if (bytes === 0) return "0 B";

    const units = ["B", "KB", "MB", "GB", "TB"];

    // A.I. helped with this calculation.
    const unitIndex = Math.min(
        Math.floor(Math.log(bytes) / Math.log(1024)),
        units.length - 1
    );

    const value = bytes / (1024 ** unitIndex);

    return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ` +
        units[unitIndex];    
}
function formatDate(date: string): string {
    // A.I. helped with this.
    return new Intl.DateTimeFormat(
        undefined,
        {
            dateStyle: "medium",
            timeStyle: "short"
        }
    ).format(new Date(date));
}

window.addEventListener("popstate", () => {
    void loadCurrentState();
})

searchForm.addEventListener("submit", event => {
    event.preventDefault();

    const query = searchInput.value.trim();
    const path = getPathFromURL();

    navigate(path, query);
});

clearSearchButton.addEventListener("click", () => {
    navigate(getPathFromURL());
})

uploadForm.addEventListener("submit", event => {
    event.preventDefault();

    const file = uploadInput.files?.[0];

    if (file === undefined) {
        statusMessage.textContent = "Please select a file to upload.";
        return;
    }

    const maxUploadSize = 25 * 1024 * 1024; // 25 MB

    if (file.size > maxUploadSize) {
        statusMessage.textContent = `The selected file exceeds the maximum upload size of ${maxUploadSize} MB.`;
        return;
    }

    void uploadFile(getPathFromURL(), file);

});

void loadCurrentState();