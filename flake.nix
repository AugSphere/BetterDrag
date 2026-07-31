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
          # Bring xdg data dirs of build inputs into the environement
          xdg_inputs=( "''${buildInputs[@]}" )
          for p in ''${xdg_inputs[@]}; do
            if [[ -d "$p/share" ]]; then
              XDG_DATA_DIRS="''${XDG_DATA_DIRS}''${XDG_DATA_DIRS+:}$p/share"
            fi
          done
          export XDG_DATA_DIRS

          dotnet tool restore
          dotnet husky install
        '';
      };
    };
}
