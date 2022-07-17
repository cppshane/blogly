# blogly

## Workflow
- Open shaneduffy workspace
- Run "Launch Workspace" in project (launch site, initialize API and MongoDB containers)
- Open ~/Workspace/my-file.html in Vim in 1st Terminal, for editing
- Open ~/Projects/Blogly in 2nd Terminal, for core commands

## Commands

### - workspace subId workspaceDir
Move post HTML from workspaceDir to posts_dev
```
dotnet run -- workspace 3 ~/Workspace
```

### - new postUri workspaceDir routesPath title type
Create new post in dev database, and create new empty file in workspace
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

Inline Code - <span class="text-code">...</span>

Text Link - <a class="text-link">....</a>
