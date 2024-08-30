use crate::constants::chunk::{CHUNK_SIZE, CHUNK_SIZEF};
use noise::*;
use std::fmt::Write;

const CAVE_SCALE: f64 = 12.0;
pub struct Cave {
    perlin: Fbm<Perlin>,
    pub buffer: [[f64; 16]; 16],
}

impl Cave {
    pub fn new(seed: u32) -> Cave {
        Cave {
            perlin: Fbm::<Perlin>::new(seed).set_frequency(0.5).set_octaves(2),
            buffer: [[0.0; CHUNK_SIZE]; CHUNK_SIZE],
        }
    }

    pub fn get_value(&self, x: f64, y: f64, z: f64) -> f64 {
        // This is a placeholder for the actual cave generation code
        self.perlin
            .get([x / CAVE_SCALE, y / CAVE_SCALE, z / CAVE_SCALE])
    }

    pub fn get_chunk(&self, x: f64, y: f64, z: f64) -> String {
        let mut output = String::new();

        for i in 0..CHUNK_SIZE {
            for j in 0..CHUNK_SIZE {
                for k in 0..CHUNK_SIZE {
                    let value = self.get_value((x * CHUNK_SIZEF) + i as f64, (y * CHUNK_SIZEF) + j as f64, (z * CHUNK_SIZEF) + k as f64);
                    write!(&mut output, "{:.6} ", value).unwrap();
                }
            }
        }

        output.trim_end().to_string()
    }
}
