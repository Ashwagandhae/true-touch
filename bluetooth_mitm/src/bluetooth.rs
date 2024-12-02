use std::future::Future;
use std::sync::Arc;

use async_stream::stream;
use btleplug::api::{
    Central, CharPropFlags, Characteristic, Manager as _, Peripheral as _, ScanFilter,
    ValueNotification,
};
use btleplug::platform::Manager;
use btleplug::platform::{Adapter, Peripheral};
use futures::lock::Mutex;
use futures::stream::StreamExt;
use futures::stream::{BoxStream, Stream};
use std::error::Error;
use uuid::Uuid;

use tokio::time::{self, Duration};

use crate::device_connection::{DeviceConnection, DeviceReadSender, DeviceReader, DeviceWriter};
use crate::hand::HandPos;

#[derive(Debug)]
pub struct Bluetooth {}

impl DeviceConnection for Bluetooth {
    type Reader = Reader;
    type Writer = Writer;

    async fn reader_writer() -> (Self::Reader, Self::Writer) {
        reader_writer_fail()
            .await
            .expect("failed to make reader writer for bluetooth")
    }
}

pub struct Reader {
    peripheral: Peripheral,
    characteristic: Characteristic,
}

impl DeviceReader for Reader {
    async fn read(&mut self, mut sender: DeviceReadSender) -> () {
        println!(
            "Subscribing to characteristic {:?}",
            self.characteristic.uuid
        );
        self.peripheral
            .subscribe(&self.characteristic)
            .await
            .unwrap();
        let mut notification_stream = self.peripheral.notifications().await.unwrap().take(1000000);

        while let Some(data) = { notification_stream.next().await } {
            let val = String::from_utf8(data.value.clone())
                .unwrap_or_else(|_| format!("fail parse : {:?}", data.value));
            println!("Received data from {:?} [{:?}]", data.uuid, val);

            //TODO convert bluetooth data to HandPos
            sender.send(HandPos::default()).await;
        }
    }
}

pub struct Writer {}

impl DeviceWriter for Writer {
    async fn write(&self, command: crate::hand::HandCommand) -> () {
        println!("pretended to write");
    }
}

/// Only devices whose name contains this string will be tried.
const PERIPHERAL_NAME_MATCH_FILTER: &str = "ESP32";
/// UUID of the characteristic for which we should subscribe to notifications.
const NOTIFY_CHARACTERISTIC_UUID: Uuid = Uuid::from_u128(0x6e400001_b5a3_f393_e0a9_e50e24dcca9e);

const SECONDS_TO_SCAN: u64 = 5;

async fn reader_writer_fail() -> Result<(Reader, Writer), Box<dyn Error>> {
    let manager = Manager::new().await?;
    let adapter_list = manager.adapters().await?;
    if adapter_list.is_empty() {
        eprintln!("No Bluetooth adapters found");
    }

    for adapter in adapter_list.iter() {
        println!("Starting scan... (will take {SECONDS_TO_SCAN} seconds)");
        adapter
            .start_scan(ScanFilter::default())
            .await
            .expect("Can't scan BLE adapter for connected devices...");
        time::sleep(Duration::from_secs(SECONDS_TO_SCAN)).await;
        let peripherals = adapter.peripherals().await?;

        if peripherals.is_empty() {
            eprintln!("->>> BLE peripheral devices were not found, sorry. Exiting...");
        } else {
            // All peripheral devices in range.
            for peripheral in peripherals {
                let properties = peripheral.properties().await?;
                let is_connected = peripheral.is_connected().await?;
                let local_name = properties
                    .unwrap()
                    .local_name
                    .unwrap_or(String::from("(peripheral name unknown)"));

                println!(
                    "Peripheral {:?} is connected: {:?}",
                    &local_name, is_connected
                );
                // Check if it's the peripheral we want.
                if local_name.contains(PERIPHERAL_NAME_MATCH_FILTER) {
                    println!("Found matching peripheral {:?}...", &local_name);
                    if !is_connected {
                        // Connect if we aren't already connected.
                        if let Err(err) = peripheral.connect().await {
                            eprintln!("Error connecting to peripheral, skipping: {}", err);
                            continue;
                        }
                    }
                    let is_connected = peripheral.is_connected().await?;
                    println!(
                        "Now connected ({:?}) to peripheral {:?}.",
                        is_connected, &local_name
                    );
                    if is_connected {
                        println!("Discover peripheral {:?} services...", local_name);
                        peripheral.discover_services().await?;
                        for characteristic in peripheral.characteristics() {
                            println!("Checking characteristic {:?}", characteristic);
                            // Subscribe to notifications from the characteristic with the selected
                            // UUID.
                            if characteristic.uuid == NOTIFY_CHARACTERISTIC_UUID
                                && characteristic.properties.contains(CharPropFlags::NOTIFY)
                            {
                                return Ok((
                                    Reader {
                                        characteristic,
                                        peripheral,
                                    },
                                    Writer {},
                                ));
                                // peripheral.subscribe(&characteristic).await?;
                                // // Print the first 4 notifications received.
                                // let mut notification_stream =
                                //     peripheral.notifications().await?.take(1000000);
                                // // Process while the BLE connection is not broken or stopped.
                                // while let Some(data) = notification_stream.next().await {
                                //     let val = String::from_utf8(data.value.clone())
                                //         .unwrap_or(format!("fail parse : {:?}", data.value));
                                //     println!(
                                //         "Received data from {:?} [{:?}]: {:?}",
                                //         local_name, data.uuid, val
                                //     );
                                // }
                            }
                        }
                        println!("Disconnecting from peripheral {:?}...", local_name);
                        peripheral.disconnect().await?;
                    }
                } else {
                    println!("Skipping unknown peripheral {:?}", local_name);
                }
            }
        }
    }
    panic!("failed to find bluetooth")
}