# blogly

## Workflow
- Run docker-compose for shaneduffy.io API and Site folders
```
docker-compose -f docker-compose.dev.yml up --build
```
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
Creates new post in local dev db and creates file in workspace.
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
