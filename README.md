# csharp-SimpleLibraries
Collection of small - single-".cs" libraries.

----
## Kirides.Libs.Events.EventAggregator

After having the need for a simple loosely coupled eventing system, and a little bit of boredom i decided to look up what it needs to create an simple EventAggregator implementation.

It ..
- supports any type of class for its payload (also primitive types!).
- can unsubscribe via an ISubscriptionToken or via the callback you used while subscribing.
- is threadsafe.
- can handle UI-Dispatching for WPF (System.Windows.Threading.Dispatcher) and Windows Forms (SychronizationContext, limited to thread on which the subscription was made).
- has no external dependencies (WindowsBase.dll and PresentationFramework.dll for Dispatcher.).
- built for .Net Framework 4+ (ConcurrentDictionary<K,V>).

```csharp
// IEventAggregator:
ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback);
ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback, ThreadOption threadOption);
void Publish<TPayload>() where TPayload : new();
void Publish<TPayload>(TPayload payload);
void Unsubscribe<TPayload>(Action<TPayload> callback);
void Unsubscribe(ISubscriptionToken token);

// ISubscriptionToken
void Dispose();
```

Credits to "user1548266" and "mike z" from StackOverflow  
- "user1548266" for having ask the question: "WeakReference is dead"  
- "mike z" for the implementation of the "Subscriber.Invoke<>"-Method.  
This part kept me going for days until i found out that the Compiler does funny stuff with the handed Callbacks.  

Reference: http://stackoverflow.com/questions/20169296/weakreference-is-dead

----
## Kirides.Libs.Configuration.IniConfig
A relatively small and easy to use Ini-File class

It..
- can return values with a different type than string, using the `IIniSection.GetValue<T>(...)`-Method.
- can read and write sections/keys/values.
- supports the following syntax: `IIniConfig["Section"]["Key"]` for accessing and altering the value of a key.
alternatively you can use the `IIniConfig.GetSection("Section").GetValue<T>("Key")`-syntax.

```csharp
//IniConfig:
public static IIniConfig FromFile(string filePath) {...}
public static IIniConfig FromString(string ini) {...}

// IIniConfig:
IIniSection this[string section] { get; }
IIniSection GetSection(string section);
void RemoveSection(string section);
IIniSection AddSection(string section);
void SaveToFile(string fileName);

// IIniSection
string Name { get; }
IDictionary<string, object> KeyValuePairs { get; }
T GetValue<T>(string key);
string GetValue(string key);
string this[string key] { get; set; }
void AddOrReplace(string key, object value);
void AddOrReplaceKey(string key, object value = null);
void RemoveKey(string key);
```
----
## Kirides.Libs.Extensions.IQueryable.Search.IQueryableSearchExtensions
Allows for Full-Text-Search of a given Type or Property/Field.
It uses Expression-Trees and is compatible (in that it does the computation on the Db-side) with (few tested) ORM's (MongoDb.Driver, EntityFramework, ...)

```csharp
//DEMO

IQueryable<File> files = ...;
IQueryable<File> filesWithTest = files.FullTextSearch<File>(x => Name, "Test", exactMatch: false, matchAllWords: true);
/*
  File:
  {
    Name: "Production App Test.exe"
  },
  {
    Name: "Production App Test.exe"
  }
*/

IQueryable<Log> logs = ...;
IQueryable<Log> logsWithError_404 = files.FullTextSearch<Log>("Error 404", exactMatch: false, matchAllWords: true);
/*
  Log:
  { // Found because either "ShortDescription" or "Content" contained "Error" and "404"
    ShortDescription: "Error code 404",
    Content: "Website responded with Error code 404"
  },
  {
    ShortDescription: "Error code 404", // Found because "ShortDescription" contained "Error" and "404"
    Content: "Page could not be found"
  }
*/
```
----
## Kirides.Libs.Extensions.IQueryable.Sorting.SortExtensions
Allows for dynamic (string based) sorting of `IQueryable<TSource>`.  
Supports nested properties. (No `IEnumerable<T>`-like properties)
```cs
IQueryable<User> users = ...;
IOrderedQueryable<Users> usersById = users.SortBy("Name");
IOrderedQueryable<Users> usersByIdThenDateDay = usersById.ThenSortBy("Date.Day");

List<User> orderedUsers = usersByIdThenDateDay.ToList();

class User
{
    public string Name { get; set; }
    public DateTime Date { get; set; }
}
```
----
## Kirides.Libs.Extensions.IO.ByteSizeExtensions
Humanizing of bytesizes for common datatypes used for sizes. (`double`, `float`, `long`, `int`)

```cs
double fileSize = 1024;
string humanFileSize = fileSize.Humanize();
// "1 KiB"
string humanFileSizeSi = fileSize.Humanize(si: true);
// "1,02 kB"
```