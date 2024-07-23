# LibLoadGenerator
- Do you hate dlls?
- Do you like shitty unreadable code?
- Do you want to add code to your project that no one will understand?
- Do you like complicated solutions for problems that no one ever had?

If you answered any of these questions with yes, then this tool is for you!

It can reduce the "dll hell problem" by converting all dlls to base64, zipping them for good measure and directly adding them to the project with a class. All that will be done by the tool automatically, all you have to do is provide the dll files and copy the generated code to your project.

# Usage Guide
1) Copy all dlls that you want to load dynamically to the path of the tool
2) Run The command ``LibLoadGenerator first.dll second.dll ...``
3) Copy the content of the output.txt file to your project
4) Run the code by adding the line ``LibLoader.Loader.Load();`` to your program. It has to be called before the main method, so you have to create a static constructor (See the examples below)
5) Now the program works without the dlls being present in its directory. Note that this currently only works with managed .net dlls

# Examples

Console App:
```cs
class Program
{
  static Program() => LibLoader.Loader.Load();

  static void Main() {
    //Program code
  }
}
```

WPF App:
```cs
partial class App
{
  static App() => LibLoader.Loader.Load();
}
```
