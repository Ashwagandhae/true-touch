# true touch unity connection

It uses a rust script to read bluetooth data and sends it to unity, because macos unity bluetooth plugin doesn't work or costs money.

## how to use

1. Run the rust script:

```bash
cd bluetooth_mitm
cargo run
```

2. Open unity editor, go to scenes, and start Sample Scene.

If you don't have any real bluetooth and want to just test the hand visualization in unity, run the rust script with the `-f` flag to send simulated data instead of reading from bluetooth. So instead of running

```bash
cargo run
```

do

```bash
cargo run -- -f
```
