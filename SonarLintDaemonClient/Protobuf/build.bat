"..\..\packages\Grpc.Tools.1.2.2\tools\windows_x64\protoc.exe" -I=. --csharp_out . --grpc_out . ./sonarlint-daemon.proto --plugin=protoc-gen-grpc=../../packages/Grpc.Tools.1.2.2/tools/windows_x64/grpc_csharp_plugin.exe
