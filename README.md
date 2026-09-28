# Jellyfin Web File Manager

A Jellyfin 12.1 plugin that provides an authenticated web file manager for a configured root directory, defaulting to /media.

## Features

- Browse files and folders
- Upload files
- Download files
- Create folders
- Rename files and folders
- Move API support
- Delete files and folders
- Path traversal protection
- Jellyfin-authenticated API

## Build

Requires .NET 10 SDK.

\`\`\`bash
dotnet restore
dotnet build -c Release
\`\`\`

GitHub Actions builds the plugin and creates a ZIP artifact.

## Default root

The plugin defaults to:

\`\`\`
/media
\`\`\`

Change the plugin configuration if the Jellyfin media directory is mounted elsewhere.
