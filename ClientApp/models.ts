export interface FileSystemItem {
    name: string,
    path: string,
    isDirectory: boolean,
    size: number | null,
    lastModifiedUtc: string;
}
export interface ViewSummary {
    fileCount: number,
    folderCount: number,
    totalFileSize: number;
}
export interface BrowseResponse {
    path: string,
    parentPath: string | null,
    items: FileSystemItem[],
    summary: ViewSummary;
}
export interface SearchResponse {
    path: string,
    query: string,
    items: FileSystemItem[],
    summary: ViewSummary,
    isTruncated: boolean;
}
export interface FileBrowserSettings {
    maximumUploadSizeInBytes: number;
    maximumSearchResults: number;
}