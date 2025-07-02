# blogly

Write blog post in HTML, output to Markdown and publish to main site + 3rd party sites

## Blog Flow

1. Start [personal site](https://github.com/cppshane/shaneduffy) > (on personal site project directory) `docker compose up`
2. Ensure it shows up properly at `localhost:4200`
3. Import posts from server if necessary > `sudo bash blogly-import.sh`
4. Create new post > `dotnet run -- new`
5. Write post in workspace document created
6. Migrate to server > `sudo bash blogly-migrate.sh`
7. Crosspost to sites > `sudo bash crosspost.sh <postId>`
8. Verify posts on crosspost sites (Finish Medium publication, manually add image captions, review for typos, etc.)
9. Regenerate routes.txt/sitemap > (on server) `bash blogly-generate.sh`
10. Pre-render > (on server) `bash deploy-shaneduffy.sh`
11. Backup to G-Drive > (on server) `bash backup-mongo.sh`

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
dotnet run -- migrate "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database" "mongodb://shane:password@143.198.140.108:27017/shaneduffy_database?authSource=shaneduffy_database"
```

### - crosspost platform(medium, dev, hashnode) subId workspaceDir
Migrates the post with the given subId to the specified platform.
```
dotnet run -- crosspost medium 13 /home/shane/workspace
```

## Notes
type - blog, notes

postUri - does not contain file extension

### Styles/Elements
`h1`, `h2`, `h3`, `h4`, `p`

Inline Code - `<span class="text-code">...</span>`

Full Code - `<pre><code>...</pre></code>`

Text Link - `<a class="text-link">....</a>`

Italic - `<span class="text-italic">...</span>`

### Code Highlighting
The value in `codelang` attribute will be forwarded to the Markdown translation.
```
<pre><code codelang="cs"></code></pre>
```
