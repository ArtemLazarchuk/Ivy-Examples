using ColorChanger.Apps;
using Ivy;

var server = new Server();
server.SetMetaTitle("Color Changer");
server.SetMetaDescription("A simple interactive color picker app demonstrating state management in Ivy.");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(ColorChangerApp));
await server.RunAsync();
