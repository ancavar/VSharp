namespace VSharp.System

open global.System
open VSharp
open VSharp.Core

// ------------------------------- mscorlib.System.Environment -------------------------------

module internal Environment =

    [<Implements("System.String System.Environment.GetResourceFromDefault(System.String)")>]
    val GetResourceFromDefault : state -> term list -> term

    [<Implements("System.Int32 System.Environment.get_CurrentManagedThreadId()")>]
    val GetCurrentManagedThreadId : state -> term list -> term

    [<Implements("System.Int32 System.Threading.Thread.get_ManagedThreadId(this)")>]
    val GetManagedThreadId : state -> term list -> term

    [<Implements("System.Void System.Console.WriteLine(System.String)")>]
    [<Implements("System.Void System.Console.WriteLine(System.Object)")>]
    val WriteLine : state -> term list -> term

    [<Implements("System.Boolean System.Console.get_IsOutputRedirected()")>]
    val get_IsOutputRedirected : state -> term list -> term

    [<Implements("System.Void System.Console.Clear()")>]
    val consoleClear : state -> term list -> term

    [<Implements("System.IO.DirectoryInfo System.IO.Directory.CreateDirectory(System.String)")>]
    val CreateDirectory : state -> term list -> term

    [<Implements("System.Boolean System.IO.File.Exists(System.String)")>]
    val FileExists : state -> term list -> term
