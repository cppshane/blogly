# blogly

## Commands

### - listen subId workspaceDir
Listen for changes to a specific file within the workspace
```
dotnet run -- listen 3 ~/Workspace
```

### - new
Creates new post in local dev db and creates file in workspace, opens the file in VSCode and then begins listening for changes to file.
```
dotnet run -- new
```

### - migrate connectionString1 connectionString2
Adds all non-existing posts from one database to another.
```
dotnet run -- migrate "mongodb://shane:password@172.17.0.1:27017/shaneduffy_database?authSource=shaneduffy_database" "mongodb://shane:password@143.198.140.108:27017/shaneduffy_database?authSource=shaneduffy_database"
```

## Notes
type - blog, notes

postUri - does not contain file extension

### Styling Classes

Inline Code - `<span class="text-code">...</span>`

Full Code - `<pre><code>...</pre></code>`

Text Link - `<a class="text-link">....</a>`

Italic - `<span class="text-italic">...</span>`
