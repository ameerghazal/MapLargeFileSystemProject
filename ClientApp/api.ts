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
        let message = `API request failed with status ${ response.status }.`

        try {
            const body = await response.json();

            if (typeof body?.error === "string")
                message = body.error

        } catch {}

        throw new Error(message);
    }

    if (response.status === 204) return undefined as T;

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
export function deleteFile(path: string): Promise<void> {
    const param = new URLSearchParams({ path });
    return apiRequest<void>(
        `/api/files/delete?${param}`,
        { method: "DELETE" }
    );
}
export function getSettings(): Promise<FileBrowserSettings> {
    return apiRequest<FileBrowserSettings>("/api/files/settings");
}