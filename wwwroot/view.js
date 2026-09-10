import { formatBytes, formatDate, formatCount } from "./helpers.js";
import { navigate } from "./navigation.js";
function getElement(selector) {
    const element = document.querySelector(selector);
    if (element == null) {
        throw new Error(`Element ${selector} was not found.`);
    }
    return element;
}
const fileList = getElement("#file-list");
const folderCount = getElement("#folder-count");
const fileCount = getElement("#file-count");
const totalSize = getElement("#total-size");
const statusMessage = getElement("#status-message");
const breadcrumbs = getElement("#breadcrumbs");
export const searchForm = getElement("#search-form");
export const searchInput = getElement("#search-input");
export const clearSearchButton = getElement("#clear-search");
export const uploadForm = getElement("#upload-form");
export const uploadInput = getElement("#upload-input");
export const uploadButton = getElement("#upload-button");
export const uploadStatus = getElement("#upload-status");
export const uploadLimit = getElement("#upload-limit");
export function renderLoading(path) {
    fileList.setAttribute("aria-busy", "true");
    fileList.replaceChildren();
    renderSummary({ folderCount: 0, fileCount: 0, totalFileSize: 0 });
    renderCurrentPath(path);
    statusMessage.textContent = "Loading directory...";
}
export function renderError(message) {
    fileList.setAttribute("aria-busy", "false");
    fileList.replaceChildren(createMessageRow("Unable to load items."));
    renderSummary({ folderCount: 0, fileCount: 0, totalFileSize: 0 });
    statusMessage.textContent = message;
}
export function renderResults(response) {
    const isSearch = "query" in response;
    renderItems(response.items, isSearch);
    renderSummary(response.summary);
    renderCurrentPath(response.path);
    const countMessage = formatCount(response.summary.folderCount, response.summary.fileCount);
    if (isSearch) {
        statusMessage.textContent = `Found ${countMessage} matching "${response.query}".`
            + (response.isTruncated ? ` Showing ${response.items.length.toLocaleString()} results.` : "");
        return;
    }
    statusMessage.textContent = `Showing ${countMessage}.`;
}
function createMessageRow(message) {
    const row = document.createElement("tr");
    const cell = createTextCell(message);
    cell.colSpan = 5;
    row.append(cell);
    return row;
}
function createNameCell(item, showPath) {
    const cell = document.createElement("td");
    const container = document.createElement("span");
    container.className = "file-name";
    const icon = document.createElement("span");
    icon.className = "icon";
    icon.textContent = item.isDirectory ? "📁" : "📄";
    icon.setAttribute("aria-hidden", "true");
    const name = item.isDirectory
        ? createNavigationButton(item.name, item.path, "folder-name")
        : document.createElement("span");
    name.textContent = item.name;
    container.append(icon, name);
    cell.append(container);
    if (showPath) {
        const path = document.createElement("small");
        path.className = "result-path";
        path.textContent = item.path;
        cell.append(path);
    }
    return cell;
}
function createTextCell(text) {
    const cell = document.createElement("td");
    cell.textContent = text;
    return cell;
}
function createNavigationButton(label, path, className) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = className ?? "";
    button.textContent = label;
    button.addEventListener("click", () => navigate(path));
    return button;
}
function renderItems(items, isSearch) {
    fileList.setAttribute("aria-busy", "false");
    fileList.replaceChildren();
    if (items.length === 0) {
        fileList.replaceChildren(createMessageRow(isSearch ? "No matching files or folders" : "This directory is empty."));
        return;
    }
    for (const item of items) {
        const row = document.createElement("tr");
        row.append(createNameCell(item, isSearch), createTextCell(item.isDirectory ? "Folder" : "File"), createTextCell(item.size === null ? "-" : formatBytes(item.size)), createTextCell(formatDate(item.lastModifiedUtc)), createActionCell(item));
        fileList.append(row);
    }
}
function renderCurrentPath(path) {
    const homeButton = createNavigationButton("Home", "", "breadcrumb-button");
    breadcrumbs.replaceChildren(homeButton);
    const segments = path.split("/").filter(segment => segment.length > 0);
    let targetPath = "";
    let currentButton = homeButton;
    for (const segment of segments) {
        targetPath = targetPath.length === 0 ? segment : `${targetPath}/${segment}`;
        const separator = document.createElement("span");
        separator.textContent = "/";
        separator.className = "breadcrumbs-separator";
        separator.setAttribute("aria-hidden", "true");
        currentButton = createNavigationButton(segment, targetPath, "breadcrumb-button");
        breadcrumbs.append(separator, currentButton);
    }
    currentButton.setAttribute("aria-current", "location");
}
function renderSummary(summary) {
    folderCount.textContent = summary.folderCount.toLocaleString();
    fileCount.textContent = summary.fileCount.toLocaleString();
    totalSize.textContent = formatBytes(summary.totalFileSize);
}
function createActionCell(item) {
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
    link.setAttribute("aria-label", `Download ${item.name}`);
    const deleteButton = document.createElement("button");
    deleteButton.type = "button";
    deleteButton.className = "delete-button";
    deleteButton.textContent = "X";
    deleteButton.setAttribute('aria-label', `Delete ${item.name}`);
    deleteButton.addEventListener("click", () => {
        window.dispatchEvent(new CustomEvent("delete-file", {
            detail: {
                path: item.path,
                name: item.name
            }
        }));
    });
    cell.append(link, deleteButton);
    return cell;
}
