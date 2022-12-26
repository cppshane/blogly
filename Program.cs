using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace Blogly
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string primaryCommand = args[0];

            if (primaryCommand == "listen") {
                string workspaceDir = "/home/shane/workspace";
                if (args.Length >= 3) {
                    workspaceDir = args[2];
                }

                StartListening(args[1], workspaceDir);
            } else if (primaryCommand == "new") {
                string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
                
                // Title
                Console.Write("Enter Title (e.g. This Is My Post): ");
                string title = Console.ReadLine() ?? "";

                // Uri
                Console.Write("Enter Uri (e.g. this-is-my-post): ");
                string uri = Console.ReadLine() ?? "";

                // Image Uri
                Console.Write("Enter Image Uri (Not necessary):");
                string? image = Console.ReadLine();

                // Video Uri
                Console.Write("Enter Video Uri (format https://www.youtube.com/embed/my_code_here): ");
                string? video = Console.ReadLine();

                // Keywords
                Console.Write("Enter Keywords (comma-separated): ");
                string keywords = Console.ReadLine() ?? "";
                
                // Post Type
                Console.Write("Type (blog or note, default blog): ");
                string type = Console.ReadLine() ?? "";
                if (type == String.Empty) {
                    type = "blog";
                }

                // Preview
                Console.Write("Enter Preview: ");
                string preview = Console.ReadLine() ?? "";

                // Workspace Path
                Console.Write("Workspace Dir (default /home/shane/workspace): ");
                string workspaceDirectory = Console.ReadLine() ?? "/home/shane/workspace";
                if (workspaceDirectory == String.Empty) {
                    workspaceDirectory = "/home/shane/workspace";
                }

                // Connect to local database
                MongoClient dbClient = new MongoClient(connectionString);
                var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
                int i = 0;
                while (sourceCollection.Find(post => post.SubId.Equals(i)).FirstOrDefault() != null) {
                    i++;
                }
                Console.WriteLine(workspaceDirectory);
                Console.WriteLine(uri);
                
                // Create workspace file
                var path = Path.Combine(workspaceDirectory, uri + ".html");
                Console.WriteLine(path);
                System.IO.File.Create(path).Close();
                Console.WriteLine($"Created file at: {path}");

                // Open workspace file in VS Code
                Console.WriteLine($"Opening in VS Code...");
                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash" };
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();
                proc.StandardInput.WriteLine($"code {path}");
                
                // Create new post in local database
                Post post = new Post();
                post.Uri = uri;
                post.SubId = i;
                post.Title = title;
                post.Date = DateTime.Today;
                post.Type = type;
                post.Keywords = keywords.Split(",").ToList();
                post.Preview = preview;
                if (image != null) {
                    post.Image = image;
                }
                if (video != null) {
                    post.Video = video;
                }
                Console.WriteLine($"New SubId: {i}");
                sourceCollection.InsertOne(post);

                // Start listening for changes
                StartListening(i.ToString(), workspaceDirectory);
            } else if (primaryCommand == "migrate") {
                string localConnectionString = args[1];
                string remoteConnectionString = args[2];

                // Get posts from local database
                Console.WriteLine("Getting posts from source DB...");
                MongoClient localClient = new MongoClient(localConnectionString);
                var localDatabase = localClient.GetDatabase("shaneduffy_database");
                var localSourceCollection = localDatabase.GetCollection<Post>("posts");
                var localPosts = localSourceCollection.Find(p => true).ToList<Post>();

                // Add posts to remote database
                Console.WriteLine("Sending to dest DB...");
                MongoClient remoteClient = new MongoClient(remoteConnectionString);
                var remoteDatabase = remoteClient.GetDatabase("shaneduffy_database");
                var remoteSourceCollection = remoteDatabase.GetCollection<Post>("posts");
                var remotePosts = remoteSourceCollection.Find(p => true).ToList<Post>();

                // Add all posts that don't currently exist within remote database
                var postsToAdd = localPosts.Where(l => !remotePosts.Select(r => r.Id).Contains(l.Id));
                remoteSourceCollection.InsertMany(postsToAdd);
            } else if (primaryCommand == "generate") {
                string connectionString = args[1];
                string routesPath = args[2];
                string sitemapPath = args[3];

                // Get posts from local database
                MongoClient localClient = new MongoClient(connectionString);
                var localDatabase = localClient.GetDatabase("shaneduffy_database");
                var localSourceCollection = localDatabase.GetCollection<Post>("posts");
                var localPosts = localSourceCollection.Find(p => true).ToList<Post>();

                // Write routes file
                string routesContent = String.Empty;
                foreach (var post in localPosts) {
                    if (post.Type.Equals("blog") && post.Uri != null && post.Uri != "") {
                        routesContent += $"/blog/{post.Uri}";
                    } else if (post.Type.Equals("notes")) {
                        routesContent += $"/notes/{post.Uri}j";
                    }
                    routesContent += Environment.NewLine;
                }
                File.WriteAllText(routesPath, routesContent);

                // Write sitemap file
                string sitemapContent = String.Empty;
                sitemapContent += "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine;
                sitemapContent += "<urlset xlmns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" + Environment.NewLine;
                foreach (var post in localPosts) {
                    sitemapContent += "  <url>" + Environment.NewLine;
                    if (post.Type.Equals("blog")) {
                        sitemapContent += "    <loc>" + $"https://shaneduffy.io/blog/{post.Uri}" + "</loc>" + Environment.NewLine;
                    } else if (post.Type.Equals("notes")) {
                        sitemapContent += "    <loc>" + $"https://shaneduffy.io/notes/{post.Uri}" + "</loc>" + Environment.NewLine;
                    }
                    sitemapContent += "    <lastmod>" + post.Date.ToString("yyyy-MM-dd") + "</lastmod>" + Environment.NewLine;
                    sitemapContent += "  </url>" + Environment.NewLine;
                }
                sitemapContent += "</urlset>";
                File.WriteAllText(sitemapPath, sitemapContent);
            }  else if (primaryCommand == "crosspost") {
                string platform = args[1];
                string subIdString = args[2];
                string workspaceDirectory = args[3];

                string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
                int subId = Int32.Parse(subIdString);

                Console.WriteLine("Connecting to database...");
                MongoClient dbClient = new MongoClient(connectionString);
                var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
                Post post = sourceCollection.Find(post => post.SubId.Equals(subId)).FirstOrDefault();

                if (post == null) {
                    Console.WriteLine("Post not found");
                    return;
                }

                if (post.Title == null || post.Preview == null || post.Uri == null) {
                    Console.WriteLine("Post has fields that should not be null.");
                    return;
                }

                string canonUri = "https://shaneduffy.io/blog/" + post.Uri;
                var htmlFilePath = Path.Combine(workspaceDirectory, post.Uri + ".html");
                
                if (platform == "medium") {
                    string mediumUserId = args[4];
                    string mediumToken = args[5];

                    var mediumMarkdown = GetMarkdown(htmlFilePath.ToString(), post.Preview, post.Title, post.Video, MarkdownType.Medium);
                    await CreateMediumPost(mediumUserId, mediumToken, post.Title, mediumMarkdown, post.Keywords, canonUri);
                } else if (platform == "hashnode") {
                    string publicationId = args[4];
                    string hashnodeToken = args[5];

                    var hashnodeMarkdown = GetMarkdown(htmlFilePath.ToString(), post.Preview, post.Title, post.Video, MarkdownType.Hashnode);
                    await CreateHashnodePost(hashnodeToken, publicationId, post.Title, post.Uri, hashnodeMarkdown, post.Keywords, canonUri, post.Image);
                } else if (platform == "dev") {
                    string devToken = args[4];

                    var devMarkdown = GetMarkdown(htmlFilePath.ToString(), post.Preview, post.Title, post.Video, MarkdownType.Dev);
                    await CreateDevPost(devToken, post.Title, devMarkdown, post.Keywords, canonUri, post.Image);
                }
            } 
        }

        private static async Task CreateHashnodePost(string hashnodeToken, string publicationId, string title, string slug, string markdownContent, List<string> tags, string canonUri, string image) {
            var httpClient = new HttpClient();  
            httpClient.DefaultRequestHeaders.Add("Authorization", hashnodeToken);
            
            // ew graphql
            var jsonString = JsonConvert.SerializeObject(new {
                query = "mutation CreatePublicationStory {createPublicationStory(publicationId: \"" + publicationId + "\", input: { " + 
                    "title: \"" + title + "\", " + 
                    "slug: \"" + slug + "\", " + 
                    ((image != null && image != String.Empty) ? "coverImageURL: \"" + image + "\", " : String.Empty) + 
                    "isRepublished: { originalArticleURL: \"" + canonUri + "\" }," +
                    "contentMarkdown: \"" +  HttpUtility.JavaScriptStringEncode(markdownContent) + "\", " + 
                    "tags: []" +
                    "}) {code,success,message}}"
            });
            var result = await httpClient.PostAsync("https://api.hashnode.com", new StringContent(jsonString, Encoding.UTF8, "application/json"));

            if (result.IsSuccessStatusCode) {
                Console.WriteLine("Successfully uploaded to Hashnode. ADD TAGS on hashnode.");
            } else {
                Console.WriteLine("Failed to upload to Hashnode. Response Status Code: " + result.StatusCode + ", Response Message: " + (await result.Content.ReadAsStringAsync()));
            }
        }

        private static async Task CreateMediumPost(string mediumUserId, string mediumToken, string title, string markdownContent, List<string> tags, string canonUri) {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mediumToken);

            var jsonString = JsonConvert.SerializeObject(new {
                title = title,
                contentFormat = "markdown",
                content = markdownContent,
                tags = tags,
                canonicalUrl = canonUri,
                publishStatus = "draft",
                notifyFollowers = true
            });
            var body = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync("https://api.medium.com/v1/users/" + mediumUserId + "/posts", body);
;
            if (result.IsSuccessStatusCode) {
                Console.WriteLine("Successfully uploaded to Medium. DRAFT ONLY. Ensure code snippets are highlighted before publish.");
            } else {
                Console.WriteLine("Failed to upload to Medium. Response Status Code: " + result.StatusCode + ", Response Message: " + (await result.Content.ReadAsStringAsync()));
            }
        }

        private static async Task CreateDevPost(string devToken, string title, string markdownContent, List<string> tags, string canonUri, string image) {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", devToken);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 13_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");

            var jsonString = JsonConvert.SerializeObject(new {
                article = new {
                    title = title,
                    body_markdown = markdownContent,
                    tags = tags,
                    canonical_url = canonUri,
                    published = true,
                    main_image = image
                }
            });
            var body = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync("https://dev.to/api/articles", body);

            if (result.IsSuccessStatusCode) {
                Console.WriteLine("Successfully uploaded to DEV.");
            } else {
                Console.WriteLine("Failed to upload to DEV. Response Status Code: " + result.StatusCode + ", Response Message: " + (await result.Content.ReadAsStringAsync()));
            }
        }

        private static void StartListening(string subIdString, string workspaceDirectory) {
            string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
            int subId = Int32.Parse(subIdString);

            Console.WriteLine("Connecting to database...");
            MongoClient dbClient = new MongoClient(connectionString);
            var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
            Post post = sourceCollection.Find(post => post.SubId.Equals(subId)).FirstOrDefault();

            string previousContent = null;
            
            Console.WriteLine("Attempting to read file...");
            while (previousContent == null) {
                try {
                    previousContent = File.ReadAllText(Path.Combine(workspaceDirectory, post.Uri + ".html"));
                } catch (Exception e) { 
                    Console.WriteLine(e.Message);
                }
                Thread.Sleep(500);
            }

            Console.WriteLine("Listening for file changes...");
            while (true) {
                try {
                    string newContent = File.ReadAllText(Path.Combine(workspaceDirectory, post.Uri + ".html"));
                    if (newContent != previousContent) {
                        Console.WriteLine("Changes detected!");
                        post.Content = newContent;
                        sourceCollection.FindOneAndDelete(o => o.Id.Equals(post.Id));
                        sourceCollection.InsertOne(post);
                        previousContent = newContent;
                    }
                } catch (Exception e) {}

                Thread.Sleep(500);
            }
        }

        private static string GetMarkdown(string htmlFilePath, string previewText, string title, string video, MarkdownType type) {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(File.ReadAllText(htmlFilePath));
            
            string markdown = String.Empty;

            if (type == MarkdownType.Medium) {
                markdown += "# " + title + Environment.NewLine;
            }

            // Add preview
            markdown += ">" + previewText + Environment.NewLine + Environment.NewLine;

            // Add video
            if (video != null && video != String.Empty) {
                if (type == MarkdownType.Dev) {
                    markdown += "{% embed " + video + " %}" + Environment.NewLine + Environment.NewLine;
                } else if (type == MarkdownType.Hashnode) {
                    markdown += "%[" + video + "]" + Environment.NewLine + Environment.NewLine;
                } else if (type == MarkdownType.Medium) {
                    markdown += "<iframe src=\"" + video + "\" allowfullscreen></iframe>" + Environment.NewLine + Environment.NewLine;
                }
            }

            // Add content
            var nodes = document.DocumentNode.ChildNodes;
            foreach (var node in nodes) {
                if (node.Name == "p"
                || node.Name == "h1"
                || node.Name == "h2"
                || node.Name == "h3"
                || node.Name == "h4") {
                    var innerNodes = node.ChildNodes;

                    if (node.Name == "h1") {
                        markdown += "# ";
                    } else if (node.Name == "h2") {
                        markdown += "## ";
                    } else if (node.Name == "h3") {
                        markdown += "### ";
                    } else if (node.Name == "h4") {
                        markdown += "#### ";
                    }

                    foreach (var innerNode in innerNodes) {
                        if (innerNode.Name == "#text") {
                            markdown += innerNode.InnerText;
                        } else if (innerNode.Name == "a") {
                            markdown += $"[{innerNode.InnerText}]({innerNode.Attributes["href"].Value})";
                        } else if (innerNode.Name == "span" && innerNode.HasClass("text-italic")) {
                            markdown += $"*{innerNode.InnerText}*";
                        } else if (innerNode.Name == "span" && innerNode.HasClass("text-code")) {
                            markdown += $"`{innerNode.InnerText}`";
                        }
                    }
                } else if (node.Name == "pre" && node.FirstChild.Name == "code") { // <pre><code>
                    var codelang = node.FirstChild.Attributes["codelang"]?.Value ?? String.Empty;
                    markdown += "```" + codelang + Environment.NewLine;
                    markdown += WebUtility.HtmlDecode(node.FirstChild.InnerText) + Environment.NewLine;
                    markdown += "```";
                } else if (node.Name == "img") { // <img>
                    markdown += $"![{node.Attributes["alt"]?.Value ?? String.Empty}]({node.Attributes["src"].Value})";
                } else if (node.Name == "a" && node.FirstChild.Name == "img") { // <a><img>
                    markdown += $"![{node.FirstChild.Attributes["alt"]?.Value ?? String.Empty}]({node.FirstChild.Attributes["src"].Value})";
                }

                markdown += Environment.NewLine;
            }

            return markdown;
        }

        private enum MarkdownType {
            Dev,
            Hashnode,
            Medium
        }
    }

    public class Post { 
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("sub_id")]
        public int? SubId { get; set; }
        [BsonElement("uri")]
        public string? Uri { get; set; }
        [BsonElement("title")]
        public string? Title { get; set; }
        [BsonElement("type")]
        public string? Type { get; set; }
        [BsonElement("image")]
        public string? Image { get; set; }
        [BsonElement("video")]
        public string? Video { get; set; }
        [BsonElement("preview")]
        public string? Preview { get; set; }
        [BsonElement("content")]
        public string? Content { get; set; }
        [BsonElement("date")]
        public DateTime Date { get; set; }
        [BsonElement("keywords")]
        public List<string>? Keywords { get; set; }
    }
}
