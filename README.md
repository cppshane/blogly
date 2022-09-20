# blogly

## Workflow
- Run shaneduffy.io API and Site docker-compose.dev.yml
- Run blogly new
- Run blogly listen
- Open file created in Workspace and begin editing

## Commands

### - listen subId workspaceDir
Listen for changes to a specific file within the workspace
```
dotnet run -- listen 3 ~/Workspace
```

### - new
Creates new post in local dev db and creates file in workspace
```
dotnet run -- new my-file ~/Workspace ~/Projects/shaneduffy/site/routes.txt "My File" blog "keyword1,keyword2,keyword3" "My description preview, is this"
```

### - migrate connectionString1 connectionString2
Copy data from connectionString1 database to connectionString2 database
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
