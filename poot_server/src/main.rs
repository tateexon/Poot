use actix_web::{web, App, HttpServer, Responder};
use poot_server::Heightmap;

async fn height_handler(info: web::Path<(u64, f64, f64)>) -> impl Responder {
    let size: usize = 16;
    let (seed, x, y) = info.into_inner();
    // let heightmap = Heightmap::new(seed as u32, x, y);
    let heightmap = Heightmap::planet(seed as u32, x, y);

    let mut output = String::new();

    for yy in 0..size {
        for xx in 0..size {
            output.push_str(&format!("{:.6} ", heightmap.buffer[xx][yy]));
        }
        output.push('\n'); // Add a new line at the end of each row
    }
    output
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    HttpServer::new(move || {
        App::new().route("/height_two/{seed}/{x}/{y}", web::get().to(height_handler))
    })
    .bind("127.0.0.1:8080")?
    .run()
    .await
}
