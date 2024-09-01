.PHONY: build
build:
	cargo build

.PHONY: run
run: build compile-shaders-windows
	cargo run

run_debug: build compile-shaders-windows
	RUST_LOG=debug RUST_BACKTRACE=full cargo run

miri: build
	MIRIFLAGS=-Zmiri-backtrace=full cargo +nightly miri run

.PHONY: test
test:
	cargo test

.PHONY: lint
lint:
	cargo clippy -- -D warnings
	cargo fmt

.PHONY: compile-shaders-windows
compile-shaders-windows:
	glslc.exe -fshader-stage=compute .\shaders\noise.comp -o .\shaders\noise.spv
