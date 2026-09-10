import type {
    BrowseResponse,
    SearchResponse,
    FileSystemItem,
    FileBrowserSettings
} from "./models.js";

async function apiRequest<T>(url: string, options?: RequestInit): Promise<T> {
    const headers = new Headers(options?.headers);
    headers.set("Accept", "application/json");

    const response = await fetch(url, { ...options, headers });
    

    if (!response.ok) {
        throw new Error(`API request failed with status ${response.status}`);
    }

    return await response.json() as T;
}
export function browseDirectory(path: string): Promise<BrowseResponse> {
    const param = new URLSearchParams({ path });
    return apiRequest<BrowseResponse>(`/api/files/browse?${param}`);
}
export function searchDirectory(path: string, query: string): Promise<SearchResponse> {
    const param = new URLSearchParams({ path, query });
    return apiRequest<SearchResponse>(`/api/files/search?${param}`)
}
export function uploadFile(path: string, file: File): Promise<FileSystemItem> {
    const formData = new FormData();
    formData.append("file", file);

    const param = new URLSearchParams({ path });
    return apiRequest<FileSystemItem>(`/api/files/upload?${param}`, {
        method: "POST",
        body: formData
    });
}
export function getSettings(): Promise<FileBrowserSettings> {
    return apiRequest<FileBrowserSettings>("/api/files/settings");
}