use crate::terrain::{cave::Cave, heightmap::Heightmap};
use actix_web::{web, Responder};

pub async fn height_handler(info: web::Path<(u64, f64, f64)>) -> impl Responder {
    let (seed, x, y) = info.into_inner();
    let heightmap = Heightmap::planet(seed as u32, x, y);

    heightmap.format_output()
}

pub async fn cave_handler(info: web::Path<(u64, f64, f64, f64)>) -> impl Responder {
    let (seed, x, y, z) = info.into_inner();
    let cave = Cave::new(seed as u32);

    cave.get_chunk(x, y, z)
}
