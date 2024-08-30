use actix_web::{web, App, HttpServer};
use poot_server::server::handlers::{cave_handler, height_handler};

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    HttpServer::new(move || {
        App::new()
            .route("/height/{seed}/{x}/{y}", web::get().to(height_handler))
            .route("/cave/{seed}/{x}/{y}/{z}", web::get().to(cave_handler))
    })
    .bind("127.0.0.1:8080")?
    .run()
    .await
}
