use std::{fs::read, future::Future};

use futures_core::Stream;
use futures_util::pin_mut;
use futures_util::stream::StreamExt;

use tokio::{
    io::{AsyncReadExt, AsyncWriteExt},
    net::{tcp::OwnedWriteHalf, TcpListener},
};

use crate::hand::{HandCommand, HandPos};

pub trait DeviceConnection: Sized + Sync {
    type Reader: DeviceReader;
    type Writer: DeviceWriter;
    fn reader_writer() -> impl Future<Output = (Self::Reader, Self::Writer)>;
}

pub trait DeviceWriter: Sized + Send + 'static {
    fn write(&self, command: HandCommand) -> impl Future<Output = ()> + Send + Sync;
}

pub trait DeviceReader: Sized + Send + 'static {
    fn read(&mut self, sender: DeviceReadSender) -> impl Future<Output = ()>;
}

// pub async fn run(device_connection: DeviceConnection) {

// }

pub struct DeviceReadSender {
    socket_writer: OwnedWriteHalf,
}

impl DeviceReadSender {
    pub async fn send(&mut self, message: HandPos) {
        let message_bytes = message.to_bytes();
        self.socket_writer
            .write_all(&message_bytes)
            .await
            .expect("Socket broke");
        println!("socket message sent to Unity: {:?}", &message_bytes[..]);
    }
}

pub async fn run<T: DeviceConnection>() {
    let listener = TcpListener::bind("127.0.0.1:8080").await.unwrap();
    println!("TCP server running on 127.0.0.1:8080");

    let (mut reader, writer) = T::reader_writer().await;

    println!("Awaiting unity connection");
    let (socket, _) = listener.accept().await.unwrap();
    println!("Connected!");

    let (mut socket_reader, mut socket_writer) = socket.into_split();

    let read_task = async { reader.read(DeviceReadSender { socket_writer }).await };

    let write_task = tokio::spawn(async move {
        let mut buf = vec![0; 1024];
        loop {
            match socket_reader.read(&mut buf).await {
                Ok(0) => {
                    // Connection closed
                    println!("socket connection closed by Unity");
                    break;
                }
                Ok(n) => {
                    println!("socket message received from Unity: {:?}", &buf[..n]);
                    writer.write(HandCommand::from_bytes(&buf)).await;
                }
                Err(e) => {
                    eprintln!("failed to read from socket: {}", e);
                    break;
                }
            }
        }
    });
    let (_, res2) = tokio::join!(read_task, write_task).into();
    // res1.unwrap();
    res2.unwrap();
}
