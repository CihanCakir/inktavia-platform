#r "/Users/cihancakir/.nuget/packages/miniuow/3.0.0/lib/netstandard2.0/MiniUow.dll"
var type = typeof(MiniUow.Paging.Paginate<string>);
foreach (var p in type.GetProperties()) Console.WriteLine(p.Name);
