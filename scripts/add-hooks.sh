#!/usr/bin/env bash

script_dir=$(readlink -f "${BASH_SOURCE[0]}")
script_dir="${script_dir%/*}"

cp $script_dir/hooks/** $script_dir/../.git/hooks