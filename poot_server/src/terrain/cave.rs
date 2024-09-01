use crate::constants::chunk::{CHUNK_SIZE, CHUNK_SIZEF, CHUNK_SIZEU};
use noise::*;
use std::fmt::Write;

const CAVE_SCALE: f64 = 12.0;
// const TURBULENCE_SCALE: f64 = 2.0;

pub struct Cave {
    // perlin: Fbm<Perlin>,
    simplex: Fbm<Simplex>,
    // turbulence: Turbulence<Cache<Fbm<Perlin>>, Perlin>,
    pub buffer: [[f64; CHUNK_SIZEU]; CHUNK_SIZEU],
}

impl Cave {
    pub fn new(seed: u32) -> Cave {
        // let p = Fbm::<Perlin>::new(seed).set_frequency(0.7).set_octaves(4);
        // let pc = Cache::new(p);
        Cave {
            // perlin: Fbm::<Perlin>::new(seed).set_frequency(0.7).set_octaves(2),
            simplex: Fbm::<Simplex>::new(seed).set_frequency(0.5).set_octaves(2),
            // turbulence: Turbulence::<_, Perlin>::new(pc).set_frequency(0.3).set_power(0.1),
            buffer: [[0.0; CHUNK_SIZEU]; CHUNK_SIZEU],
        }
    }

    pub fn get_value(&self, x: f64, y: f64, z: f64) -> f64 {
        // This is a placeholder for the actual cave generation code

        let nx = x / CAVE_SCALE;
        let ny = y / CAVE_SCALE;
        let nz = z / CAVE_SCALE;

        // Use the turbulence to perturb the coordinates
        // let turbulence_value = self.turbulence.get([nx, ny, nz]) * TURBULENCE_SCALE;

        // self.perlin
        // self.turbulence
        self.simplex.get([nx, ny, nz])
    }

    pub fn get_chunk(&self, x: f64, y: f64, z: f64) -> String {
        let mut output = String::new();

        for i in 0..CHUNK_SIZE {
            for j in 0..CHUNK_SIZE {
                for k in 0..CHUNK_SIZE {
                    let value = self.get_value(
                        (x * CHUNK_SIZEF) + i as f64,
                        (y * CHUNK_SIZEF) + j as f64,
                        (z * CHUNK_SIZEF) + k as f64,
                    );
                    write!(&mut output, "{:.6} ", value).unwrap();
                }
            }
        }

        output.trim_end().to_string()
    }
}
