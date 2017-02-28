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
    if (isWindows) then
        let projects = !! "./**/core/**/*.csproj"
                    ++ "./**/core/**/*.fsproj"
                    ++ "./**/contrib/cluster/**/*.csproj"
                    ++ "./**/contrib/persistence/**/*.csproj"
                    ++ "./**/contrib/**/Akka.TestKit.Xunit2.csproj"
                    -- "./**/*MultiNode*.csproj"
                    -- "./**/Akka.NodeTestRunner.csproj"
                    -- "./**/Akka.Streams.Tests.TCK.csproj"

        let runSingleProject project =
            DotNetCli.Restore
                (fun p -> 
                    { p with
                        Project = project
                        NoCache = false })

        projects |> Seq.iter (runSingleProject)
    else
        let projects = !! "./**/core/**/*.csproj"
                    ++ "./**/contrib/cluster/**/*.csproj"
                    ++ "./**/contrib/persistence/**/*.csproj"
                    ++ "./**/contrib/**/Akka.TestKit.Xunit2.csproj"
                    -- "./**/*MultiNode*.csproj"
                    -- "./**/Akka.NodeTestRunner.csproj"
                    -- "./**/Akka.Streams.Tests.TCK.csproj"
                    -- "./**/Akka.API.Tests.csproj"
                    -- "./**/Akka.Cluster.TestKit.csproj"
                    -- "./**/Akka.Remote.Tests.csproj"
                    -- "./**/Akka.Remote.TestKit.csproj"
                    -- "./**/Akka.Remote.TestKit.Tests.csproj"
                    -- "./**/Akka.DistributedData.csproj"
                    -- "./**/Akka.DistributedData.Tests.csproj"
                    -- "./**/*.Performance.csproj"
                    -- "./**/Akka.Persistence.Sqlite.csproj"
                    -- "./**/Akka.Persistence.Sqlite.Tests.csproj"

        let runSingleProject project =
            DotNetCli.Restore
                (fun p -> 
                    { p with
                        Project = project
                        NoCache = false })

        projects |> Seq.iter (runSingleProject)
)

Target "Build" (fun _ ->
    if (isWindows) then
        let projects = !! "./**/core/**/*.csproj"
                    ++ "./**/core/**/*.fsproj"
                    ++ "./**/contrib/cluster/**/*.csproj"
                    ++ "./**/contrib/persistence/**/*.csproj"
                    ++ "./**/contrib/**/Akka.TestKit.Xunit2.csproj"
                    -- "./**/*MultiNode*.csproj"
                    -- "./**/Akka.NodeTestRunner.csproj"
                    -- "./**/Akka.Streams.Tests.TCK.csproj"
                    -- "./**/Akka.FSharp.Tests.fsproj"

        let runSingleProject project =
            DotNetCli.Build
                (fun p -> 
                    { p with
                        Project = project
                        Configuration = configuration })

        projects |> Seq.iter (runSingleProject)
    else
        let projects = !! "./**/core/**/*.csproj"
                       ++ "./**/contrib/cluster/**/*.csproj"
                       ++ "./**/contrib/persistence/**/*.csproj"
                       ++ "./**/contrib/**/Akka.TestKit.Xunit2.csproj"
                       -- "./**/*MultiNode*.csproj"
                       -- "./**/Akka.NodeTestRunner.csproj"
                       -- "./**/Akka.Streams.Tests.TCK.csproj"
                       -- "./**/Akka.API.Tests.csproj"
                       -- "./**/Akka.API.Tests.csproj"
                       -- "./**/Akka.Cluster.TestKit.csproj"
                       -- "./**/Akka.Remote.Tests.csproj"
                       -- "./**/Akka.Remote.TestKit.csproj"
                       -- "./**/Akka.Remote.TestKit.Tests.csproj"
                       -- "./**/Akka.DistributedData.csproj"
                       -- "./**/Akka.DistributedData.Tests.csproj"
                       -- "./**/*.Performance.csproj"
                       -- "./**/Akka.Persistence.Sqlite.csproj"
                       -- "./**/Akka.Persistence.Sqlite.Tests.csproj"

        let runSingleProject project =
            DotNetCli.Build
                (fun p -> 
                    { p with
                        Project = project
                        Configuration = configuration })

        projects |> Seq.iter (runSingleProject)
)

Target "RunTests" (fun _ ->
    if (isWindows) then
        let projects = !! "./**/core/*.Tests.csproj"
                    ++ "./**/contrib/cluster/**/*.Tests.csproj"
                    ++ "./**/contrib/persistence/**/*.Tests.csproj"
                    ++ "./**/contrib/testkits/**/*.Tests.csproj"
                    -- "./**/Akka.Persistence.Tests.csproj"
                    -- "./**/Akka.Remote.TestKit.Tests.csproj"
                    -- "./**/Akka.Remote.Tests.csproj"
                    -- "./**/Akka.Streams.Tests.csproj"
                    -- "./**/Akka.Persistence.Sqlite.Tests.csproj"

        let runSingleProject project =
            DotNetCli.Test
                (fun p -> 
                    { p with
                        Project = project
                        Configuration = configuration })

        projects |> Seq.iter (runSingleProject)
    else
        let projects = !! "./**/core/*.Tests.csproj"
                       ++ "./**/contrib/cluster/**/*.Tests.csproj"
                       ++ "./**/contrib/persistence/**/*.Tests.csproj"
                       ++ "./**/contrib/testkits/**/*.Tests.csproj"
                       -- "./**/Akka.Persistence.Tests.csproj"
                       -- "./**/Akka.Remote.TestKit.Tests.csproj"
                       -- "./**/Akka.Remote.Tests.csproj"
                       -- "./**/Akka.Streams.Tests.csproj"
                       -- "./**/Akka.Persistence.Sqlite.Tests.csproj"
                       -- "./**/Akka.DistributedData.Tests.csproj"

        let runSingleProject project =
            DotNetCli.Test
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

// tests dependencies
"Clean" ==> "RestorePackages" ==> "Build" ==> "RunTests"

// all
"BuildRelease" ==> "All"

RunTargetOrDefault "Help"