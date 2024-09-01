use actix_web::{web, App, HttpServer};
use poot_server::server::handlers::{cave_handler, height_handler, AppState};
use poot_server::{
    store::height::HeightStore,
    vulkan::{compute::ComputeShader, hardware::Hardware},
};

use std::sync::Arc;
use std::sync::RwLock;
use tokio::signal;

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let hardware = Hardware::new();
    let shared_data = Arc::new(AppState {
        shader: RwLock::new(ComputeShader::create_shader_module(&hardware.device)),
        hardware: RwLock::new(hardware),
        height_store: RwLock::new(HeightStore::new()),
    });

    let shared_data_for_server = Arc::clone(&shared_data); // Clone the Arc to keep ownership

    let server = HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(shared_data_for_server.clone()))
            .route("/height/{seed}/{x}/{y}", web::get().to(height_handler))
            .route("/cave/{seed}/{x}/{y}/{z}", web::get().to(cave_handler))
    })
    .bind("127.0.0.1:8080")?
    .run();

    // Listen for shutdown signals (like Ctrl+C)
    let shutdown_signal = async {
        signal::ctrl_c().await.expect("Failed to listen for Ctrl+C");
    };

    // Run the server and shutdown handling in parallel
    tokio::select! {
        _ = server => {},
        _ = shutdown_signal => {
            println!("Received shutdown signal, shutting down gracefully...");
        }
    }
    let hardware = shared_data.hardware.read().unwrap();
    let shader = shared_data.shader.read().unwrap();
    unsafe { hardware.device.destroy_shader_module(*shader, None) };

    Ok(())
}
