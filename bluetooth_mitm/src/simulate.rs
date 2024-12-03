use async_stream::stream;
use futures_core::Stream;
use std::sync::Arc;
use std::time::Duration;
use tokio::sync::mpsc::{Receiver, Sender};
use tokio::sync::Mutex;
use tokio::time::{interval, Instant};

use crate::device_connection::{DeviceConnection, DeviceReadSender, DeviceReader, DeviceWriter};
use crate::hand::{HandCommand, HandPos};

#[derive(Debug, Default)]
pub struct SimulateState {
    hand_pos: HandPos,
    time: std::time::Duration,
}

impl SimulateState {
    fn read(&self) -> HandPos {
        return self.hand_pos.clone();
    }
    fn update(&mut self, delta_time: std::time::Duration) {
        self.time += delta_time;
        self.hand_pos.0[0] = ((self.time.as_millis()) as f32 / 1200.0).sin();
        self.hand_pos.0[1] = ((self.time.as_millis()) as f32 / 800.0).sin();
        self.hand_pos.0[2] = ((self.time.as_millis()) as f32 / 400.0).sin();
        self.hand_pos.0[3] = ((self.time.as_millis()) as f32 / 300.0).sin();
        self.hand_pos.0[4] = ((self.time.as_millis()) as f32 / 200.0).sin();
        self.hand_pos.0[5] = ((self.time.as_millis()) as f32 / 100.0).sin();
        self.hand_pos.0[6] = ((self.time.as_millis()) as f32 / 1600.0).sin();
        self.hand_pos.0[7] = ((self.time.as_millis()) as f32 / 500.0).sin();
        // self.hand_pos.f1.upper.1 = def.f1.upper.1 + ((self.time.as_millis()) as f32 / 1000.0).sin();
        // self.hand_pos.f1.upper.2 = def.f1.upper.2 + ((self.time.as_millis()) as f32 / 2000.0).sin();
        // self.hand_pos.f1.upper.2 =
        //     def.f1.upper.2 + ((self.time.as_millis() % 1000) as f32 / 1000.0).sin();
    }
    fn write(&mut self, _command: HandCommand) {}
}
#[derive(Debug)]
pub struct Simulate {}
impl DeviceConnection for Simulate {
    type Reader = Reader;
    type Writer = Writer;
    async fn reader_writer() -> (Self::Reader, Self::Writer) {
        let (command_tx, mut command_rx) = tokio::sync::mpsc::channel(10);
        let (pos_tx, pos_rx) = tokio::sync::mpsc::channel(10);

        let state: Arc<Mutex<SimulateState>> = Arc::new(Mutex::new(Default::default()));

        {
            let state = state.clone();
            tokio::spawn(async move {
                let state = state.clone();
                loop {
                    let command = command_rx.recv().await.expect("Channel closed");
                    {
                        let mut state = state.lock().await;
                        state.write(command);
                    }
                }
            });
        }
        {
            let state = state.clone();
            tokio::spawn(async move {
                let mut wait = interval(Duration::from_millis(100));
                let mut last_time = Instant::now();
                loop {
                    wait.tick().await;
                    let current_time = Instant::now();
                    let delta_time = current_time.duration_since(last_time);
                    {
                        let mut state = state.lock().await;

                        state.update(delta_time);
                        pos_tx
                            .send(state.read())
                            .await
                            .expect("Failed to send HandPos from simulation")
                    }
                    last_time = current_time;
                }
            })
        };

        (Reader::new(pos_rx), Writer::new(command_tx))
    }
}

pub struct Reader {
    receiver: Receiver<HandPos>,
}
impl Reader {
    fn new(receiver: Receiver<HandPos>) -> Self {
        Self { receiver }
    }
}
impl DeviceReader for Reader {
    // async fn read(&self) -> impl Stream<Item = HandPos> {

    //         loop {
    //             let next = self.receiver.recv().await;
    //             yield next.expect("Channel closed");
    //         }
    // }
    async fn read(&mut self, mut sender: DeviceReadSender) -> () {
        loop {
            let next = self.receiver.recv().await;
            sender.send(next.expect("Channel closed")).await;
        }
    }
}
pub struct Writer {
    sender: Sender<HandCommand>,
}
impl DeviceWriter for Writer {
    async fn write(&mut self, command: HandCommand) {
        self.sender
            .send(command)
            .await
            .expect("Failed to send HandCommand to simulation")
    }
}
impl Writer {
    fn new(sender: Sender<HandCommand>) -> Self {
        Self { sender }
    }
}
