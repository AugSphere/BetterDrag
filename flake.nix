{
  description = ".NET 10 Development Environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs =
    { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        buildInputs = with pkgs; [
          roslyn-ls
          dotnetCorePackages.sdk_10_0
        ];
        shellHook = ''
          echo "Restoring .NET local tools..."
          dotnet tool restore
        '';
      };
    };
}
