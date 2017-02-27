#I @"tools/FAKE/tools"
#r "FakeLib.dll"

open System
open System.IO
open System.Text

open Fake
open Fake.DotNetCli

// Variables
let configuration = "Release"

// Directories
let output = __SOURCE_DIRECTORY__  @@ "build"
let outputTests = output @@ "TestResults"
let outputPerfTests = output @@ "perf"
let outputBinaries = output @@ "binaries"
let outputNuGet = output @@ "nuget"
let outputBinariesNet45 = outputBinaries @@ "net45"
let outputBinariesNetStandard = outputBinaries @@ "netstandard1.6"

Target "Clean" (fun _ ->
    CleanDir output
    CleanDir outputTests
    CleanDir outputPerfTests
    CleanDir outputBinaries
    CleanDir outputNuGet
    CleanDir outputBinariesNet45
    CleanDir outputBinariesNetStandard

    CleanDirs !! "./**/bin"
    CleanDirs !! "./**/obj"
)

Target "RestorePackages" (fun _ ->
    let projects = !! "./**/Akka.csproj"
                   ++ "./**/Akka.Remote.csproj"
                   ++ "./**/Akka.Cluster.csproj"
                   ++ "./**/Akka.Cluster.Tools.csproj"
                   ++ "./**/Akka.Cluster.Sharding.csproj"
                   ++ "./**/Akka.Persistence.csproj"
                   ++ "./**/Akka.TestKit.csproj"
                   ++ "./**/Akka.TestKit.Xunit2.csproj"
                   ++ "./**/Akka.DistributedData.csproj"

    let runSingleProject project =
        DotNetCli.Restore
            (fun p -> 
                { p with
                    Project = project
                    NoCache = false })

    projects |> Seq.iter (runSingleProject)
)

Target "Build" (fun _ ->
    let projects = !! "./**/Akka.csproj"
                   ++ "./**/Akka.Remote.csproj"
                   ++ "./**/Akka.Cluster.csproj"
                   ++ "./**/Akka.Cluster.Tools.csproj"
                   ++ "./**/Akka.Cluster.Sharding.csproj"
                   ++ "./**/Akka.Persistence.csproj"
                   ++ "./**/Akka.TestKit.csproj"
                   ++ "./**/Akka.TestKit.Xunit2.csproj"
                   ++ "./**/Akka.DistributedData.csproj"

    let runSingleProject project =
        DotNetCli.Build
            (fun p -> 
                { p with
                    Project = project
                    Configuration = configuration })

    projects |> Seq.iter (runSingleProject)
)

//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "/build [target]"
      ""
      " Targets for building:"
      " * Build      Builds"
      " * Nuget      Create and optionally publish nugets packages"
      " * RunTests   Runs tests"
      " * All        Builds, run tests, creates and optionally publish nuget packages"
      ""
      " Other Targets"
      " * Help       Display this help" 
      ""]

//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

Target "BuildRelease" DoNothing
Target "All" DoNothing

// build dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "BuildRelease"

// all
"BuildRelease" ==> "All"

RunTargetOrDefault "Help"