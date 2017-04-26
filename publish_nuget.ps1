param([string]$apikey)

dotnet pack .\src\core\Akka
dotnet pack .\src\core\Akka.Remote
dotnet pack .\src\core\Akka.Cluster
dotnet pack .\src\core\Akka.Persistence
dotnet pack .\src\contrib\cluster\Akka.Cluster.Tools
dotnet pack .\src\contrib\serializers\Akka.Serialization.Hyperion
nuget push .\src\core\Akka\bin\Debug\Akka.1.5.0.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey
nuget push .\src\core\Akka\bin\Debug\Akka.Remote.1.5.0.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey
nuget push .\src\core\Akka\bin\Debug\Akka.Cluster.1.5.0.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey
nuget push .\src\core\Akka\bin\Debug\Akka.Persistence.1.5.0.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey
nuget push .\src\contrib\cluster\Akka.Cluster\bin\Debug\Akka.Cluster.1.5.0-beta.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey
nuget push .\src\contrib\serializers\Akka.Serialization.Hyperion\bin\Debug\Akka.Serialization.Hyperion.1.5.0-beta.nupkg -Source https://www.myget.org/F/jeffpang/api/v2/package -ApiKey $apikey