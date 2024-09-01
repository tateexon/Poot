use crate::{
    constants::chunk::CHUNK_SIZE,
    store::height::HeightStore,
    terrain::cave::Cave,
    vulkan::{compute::ComputeShader, hardware::Hardware},
};
use actix_web::{web, Responder};
use ash::vk;
use std::fmt::Write;
use std::sync::Arc;

use std::sync::RwLock;

pub struct AppState {
    pub hardware: RwLock<Hardware>,
    pub shader: RwLock<vk::ShaderModule>,
    pub height_store: RwLock<HeightStore>,
}

pub async fn height_handler(
    data: web::Data<Arc<AppState>>,
    info: web::Path<(u64, f64, f64)>,
) -> impl Responder {
    let (_seed, x, y) = info.into_inner();
    let mut output = String::new();
    let hardware = data.hardware.read().unwrap();
    let shader = data.shader.read().unwrap();

    let to_gen: Vec<[i32; 2]>;

    {
        let height_store = data.height_store.read().unwrap();
        to_gen = height_store.chunks_to_generate(x as i32 * CHUNK_SIZE, y as i32 * CHUNK_SIZE);
    }
    if !to_gen.is_empty() {
        let mut height_store = data.height_store.write().unwrap();
        for [gx, gy] in to_gen {
            println!("Generating chunk: {}, {}", gx, gy);
            let cs = ComputeShader::shader_compute(&hardware, *shader, gx as f32, gy as f32);
            height_store.insert_chunks(gx * CHUNK_SIZE, gy * CHUNK_SIZE, cs.heightmap_data.clone());
        }
    }

    let height_store = data.height_store.read().unwrap();
    let chunk = height_store.get_chunk(x as i32, y as i32).unwrap();

    for val in chunk.iter() {
        write!(output, "{:.6} ", val).unwrap();
    }
    output
}

pub async fn cave_handler(info: web::Path<(u64, f64, f64, f64)>) -> impl Responder {
    let (seed, x, y, z) = info.into_inner();
    let cave = Cave::new(seed as u32);

    cave.get_chunk(x, y, z)
}
