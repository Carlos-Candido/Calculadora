#!/usr/bin/env bash
set -euo pipefail

if (( $# != 0 )); then
    printf 'Uso: bash scripts/package-desktop.sh\n' >&2
    exit 1
fi

script_path=${BASH_SOURCE[0]}
if [[ $script_path == */* ]]; then
    script_dir=${script_path%/*}
else
    script_dir=.
fi
if [[ -z $script_dir ]]; then
    script_dir=/
fi

if ! CDPATH= cd -P -- "$script_dir/.."; then
    printf 'Não foi possível localizar a raiz do projeto.\n' >&2
    exit 1
fi
root_dir=$PWD
artifacts_dir=${root_dir%/}/artifacts

if [[ -L $artifacts_dir ]]; then
    printf 'O diretório de artefatos não pode ser um link simbólico: %s\n' "$artifacts_dir" >&2
    exit 1
fi
if [[ -e $artifacts_dir && ! -d $artifacts_dir ]]; then
    printf 'O caminho de artefatos não é um diretório: %s\n' "$artifacts_dir" >&2
    exit 1
fi

for tool in dotnet zip desktop-file-validate; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        printf 'Ferramenta necessária não encontrada: %s\n' "$tool" >&2
        exit 1
    fi
done

mkdir -p -- "$artifacts_dir"
physical_artifacts_dir=$(cd -P -- "$artifacts_dir" && pwd -P && printf '.')
physical_artifacts_dir=${physical_artifacts_dir%$'\n'.}
if [[ $physical_artifacts_dir != "$artifacts_dir" ]]; then
    printf 'O diretório físico de artefatos está fora do projeto: %s\n' "$physical_artifacts_dir" >&2
    exit 1
fi

stage=
desktop_tmp=
trap 'if [[ -n $stage ]]; then rm -rf -- "$stage"; fi; if [[ -n $desktop_tmp ]]; then rm -f -- "$desktop_tmp"; fi' EXIT
stage=$(mktemp -d -- "$artifacts_dir/.package-desktop.XXXXXXXXXX" && printf '.')
stage=${stage%$'\n'.}

for rid in linux-x64 win-x64; do
    dotnet publish \
        src/Calculadora.Desktop/Calculadora.Desktop.csproj \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -p:PublishTrimmed=false \
        -p:PublishSingleFile=false \
        -o "$stage/$rid"
done

linux_app=$stage/linux-x64/Calculadora.Desktop
windows_app=$stage/win-x64/Calculadora.Desktop.exe
if [[ ! -f $linux_app || ! -x $linux_app ]]; then
    printf 'O apphost Linux não foi publicado como executável: %s\n' "$linux_app" >&2
    exit 1
fi
if [[ ! -f $windows_app ]]; then
    printf 'O executável Windows não foi publicado: %s\n' "$windows_app" >&2
    exit 1
fi

cp -- "$root_dir/packaging/windows/Criar atalho.cmd" "$stage/win-x64/Criar atalho.cmd"
(
    cd -P -- "$stage"
    zip -qr Calculadora-win-x64.zip win-x64
)

rm -rf -- \
    "$artifacts_dir/linux-x64" \
    "$artifacts_dir/win-x64" \
    "$artifacts_dir/Calculadora-win-x64.zip"
mv -- "$stage/linux-x64" "$artifacts_dir/linux-x64"
mv -- "$stage/win-x64" "$artifacts_dir/win-x64"
mv -- "$stage/Calculadora-win-x64.zip" "$artifacts_dir/Calculadora-win-x64.zip"

linux_executable=$artifacts_dir/linux-x64/Calculadora.Desktop
if [[ $linux_executable == *$'\r'* || $linux_executable == *$'\n'* ]]; then
    printf 'O caminho do executável Linux não pode conter CR ou LF: %s\n' "$linux_executable" >&2
    exit 1
fi

exec_escaped=${linux_executable//\\/\\\\}
exec_escaped=${exec_escaped//\"/\\\"}
exec_escaped=${exec_escaped//\`/\\\`}
exec_escaped=${exec_escaped//\$/\\\$}
exec_escaped=${exec_escaped//\\/\\\\}
exec_escaped=${exec_escaped//%/%%}

data_home=${XDG_DATA_HOME:-}
if [[ $data_home != /* ]]; then
    data_home=$HOME/.local/share
fi
applications_dir=$data_home/applications
desktop_file=$applications_dir/calculadora.desktop
mkdir -p -- "$applications_dir"
desktop_tmp=$(mktemp --suffix=.desktop -- "$applications_dir/.calculadora.XXXXXXXXXX" && printf '.')
desktop_tmp=${desktop_tmp%$'\n'.}
cat >"$desktop_tmp" <<EOF
[Desktop Entry]
Type=Application
Name=Calculadora
Exec="$exec_escaped"
Terminal=false
Categories=Utility;Calculator;
EOF
desktop-file-validate "$desktop_tmp"
mv -fT -- "$desktop_tmp" "$desktop_file"
desktop_tmp=

printf 'Executável Linux: %s\n' "$linux_executable"
printf 'ZIP Windows: %s\n' "$artifacts_dir/Calculadora-win-x64.zip"
printf 'Entrada de menu: %s\n' "$desktop_file"
