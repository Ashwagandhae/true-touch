// #![feature(async_closure)]

use bluetooth::Bluetooth;
use device_connection::run;
use simulate::Simulate;
use std::env;

pub mod bluetooth;
pub mod device_connection;
pub mod hand;
pub mod simulate;

#[tokio::main]
async fn main() {
    let args: Vec<String> = env::args().collect();
    let use_fake_data = args.iter().any(|arg| arg == "-f" || arg == "--fake");

    if use_fake_data {
        run::<Simulate>().await;
    } else {
        run::<Bluetooth>().await;
    }
}

// async fn run<T: SocketHandler + Send + Sync>(listener: TcpListener) {
//     // Handle incoming connections
//     let handler = T::init().await;
//     loop {
//         println!("Awaiting connection");
//         let (socket, _) = listener.accept().await.unwrap();
//         println!("Connected!");

//         handler.handle(socket).await;
//     }
// }

// pub trait SocketHandler {
//     async fn init() -> Self;
//     async fn handle(&self, socket: TcpStream);
// }
