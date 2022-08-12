using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Blogly
{
    class Program
    {
        static void Main(string[] args)
        {
            string primaryCommand = args[0];

            if (primaryCommand == "workspace") {
                string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
                int subId = Int32.Parse(args[1]);
                string workspaceDirectory = args[2];

                MongoClient dbClient = new MongoClient(connectionString);
                var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
                Post post = sourceCollection.Find(post => post.SubId.Equals(subId)).FirstOrDefault();
                
                string workspaceContent = File.ReadAllText(Path.Combine(workspaceDirectory, post.Uri + ".html"));

                post.Content = workspaceContent;
                sourceCollection.FindOneAndDelete(o => o.Id.Equals(post.Id));
                sourceCollection.InsertOne(post);
            }

            if (primaryCommand == "new") {
                string connectionString = "mongodb://shane:password@127.0.0.1:27017/shaneduffy_database?authSource=shaneduffy_database";
                string uri = args[1];
                string workspaceDirectory = args[2];
                string routesPath = args[3];
                string title = args[4];
                string type = args[5];     
                string keywords = args[6];                   
                string preview = args[7];
                string image = args[8];

                MongoClient dbClient = new MongoClient(connectionString);
                var sourceCollection = dbClient.GetDatabase("shaneduffy_database").GetCollection<Post>("posts");
                int i = 0;
                while (sourceCollection.Find(post => post.SubId.Equals(i)).FirstOrDefault() != null) {
                    i++;
                }

                System.IO.File.Create(Path.Combine(workspaceDirectory, uri + ".html"));
                    
                Post post = new Post();
                post.Uri = uri;
                post.SubId = i;
                post.Title = title;
                post.Date = DateTime.Today;
                post.Type = type;
                post.Image = image;
                post.Keywords = keywords.Split(",").ToList();
                post.Preview = preview;

                Console.WriteLine(i);

                using (StreamWriter sw = File.AppendText(routesPath)) {
                    if (post.Type.Equals("blog")) {
                        sw.WriteLine($"/blog/{uri}");
                    } else if (post.Type.Equals("notes")) {
                        sw.WriteLine($"/notes/{uri}j");
                    }
                }

                sourceCollection.InsertOne(post);
            }

            if (primaryCommand == "migrate") {
                string localConnectionString = args[1];
                string remoteConnectionString = args[2];

                // Get posts from local database
                MongoClient localClient = new MongoClient(localConnectionString);
                var localDatabase = localClient.GetDatabase("shaneduffy_database");
                var localSourceCollection = localDatabase.GetCollection<Post>("posts");
                var localPosts = localSourceCollection.Find(p => true).ToList<Post>();

                // Write posts to remote database
                MongoClient remoteClient = new MongoClient(remoteConnectionString);
                var remoteDatabase = remoteClient.GetDatabase("shaneduffy_database");
                var remoteSourceCollection = remoteDatabase.GetCollection<Post>("posts");
                remoteSourceCollection.DeleteMany(p => true);
                remoteSourceCollection.InsertMany(localPosts);
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
                    if (post.Type.Equals("blog")) {
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
    }

    public class Post { 
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("sub_id")]
        public int SubId { get; set; }
        [BsonElement("uri")]
        public string Uri { get; set; }
        [BsonElement("title")]
        public string Title { get; set; }
        [BsonElement("type")]
        public string Type { get; set; }
        [BsonElement("image")]
        public string Image { get; set; }
        [BsonElement("preview")]
        public string Preview { get; set; }
        [BsonElement("content")]
        public string Content { get; set; }
        [BsonElement("date")]
        public DateTime Date { get; set; }
        [BsonElement("keywords")]
        public List<string> Keywords { get; set; }
    }
}
