using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics;

namespace Blogly
{
    class Program
    {
        static void Main(string[] args)
        {
            string primaryCommand = args[0];

            if (primaryCommand == "listen") {
                StartListening(args[1], args[2]);
            }

            if (primaryCommand == "new") {
                string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
                
                Console.Write("Enter Title: ");
                string title = Console.ReadLine() ?? "";

                Console.Write("Enter Uri: ");
                string uri = Console.ReadLine() ?? "";

                Console.Write("Enter Image or YouTube link (https://www.youtube.com/embed/my_code_here): ");
                string? image = Console.ReadLine();

                Console.Write("Enter Keywords (comma-separated): ");
                string keywords = Console.ReadLine() ?? "";
                
                Console.Write("Type (blog or notes): ");
                string type = Console.ReadLine() ?? "";

                Console.Write("Enter Preview: ");
                string preview = Console.ReadLine() ?? "";

                Console.Write("Workspace Dir (default /home/shane/workspace): ");
                string workspaceDirectory = Console.ReadLine() ?? "/home/shane/workspace";
                if (workspaceDirectory == String.Empty) {
                    workspaceDirectory = "/home/shane/workspace";
                }

                MongoClient dbClient = new MongoClient(connectionString);
                var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
                int i = 0;
                while (sourceCollection.Find(post => post.SubId.Equals(i)).FirstOrDefault() != null) {
                    i++;
                }

                Console.WriteLine(workspaceDirectory);
                Console.WriteLine(uri);
                
                var path = Path.Combine(workspaceDirectory, uri + ".html");
                Console.WriteLine(path);

                System.IO.File.Create(path).Close();
                Console.WriteLine($"Created file at: {path}");
                Console.WriteLine($"Opening in VS Code...");

                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash" };
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();

                proc.StandardInput.WriteLine($"code {path}");
                    
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

                Console.WriteLine($"New SubId: {i}");
                sourceCollection.InsertOne(post);

                StartListening(i.ToString(), workspaceDirectory);
            }

            if (primaryCommand == "migrate") {
                string localConnectionString = args[1];
                string remoteConnectionString = args[2];

                Console.WriteLine("Getting posts from source DB...");
                // Get posts from local database
                MongoClient localClient = new MongoClient(localConnectionString);
                var localDatabase = localClient.GetDatabase("shaneduffy_database");
                var localSourceCollection = localDatabase.GetCollection<Post>("posts");
                var localPosts = localSourceCollection.Find(p => true).ToList<Post>();

                Console.WriteLine("Sending to dest DB...");
                // Add posts to remote database
                MongoClient remoteClient = new MongoClient(remoteConnectionString);
                var remoteDatabase = remoteClient.GetDatabase("shaneduffy_database");
                var remoteSourceCollection = remoteDatabase.GetCollection<Post>("posts");
                var remotePosts = remoteSourceCollection.Find(p => true).ToList<Post>();

                var postsToAdd = localPosts.Where(l => !remotePosts.Select(r => r.Id).Contains(l.Id));
                remoteSourceCollection.InsertMany(postsToAdd);
            }

            if (primaryCommand == "generate") {
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

            Console.WriteLine("Listening...");
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
