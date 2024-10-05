use crate::constants::chunk::{
    world_space_to_chunk_space_2d, world_space_to_heightmap_space_2d, CHUNK_SIZE, CHUNK_SIZEU,
    HEIGHTMAP_CHUNK_SIZE,
};
use std::collections::HashMap;
use std::sync::{Arc, RwLock};

pub struct HeightStore {
    pub map: Arc<RwLock<HashMap<[i32; 3], Vec<f32>>>>,
}

impl Default for HeightStore {
    fn default() -> Self {
        Self::new()
    }
}

impl HeightStore {
    pub fn new() -> Self {
        Self {
            map: Arc::new(RwLock::new(HashMap::new())),
        }
    }

    pub fn get_chunk(&self, x: i32, y: i32, seed: i32) -> Option<Vec<f32>> {
        // Acquire a read lock on the map
        let map = self.map.read().unwrap();
        // Attempt to get the chunk
        map.get(&[x, y, seed]).cloned()
    }

    fn append_chunk(&self, x: i32, y: i32, seed: i32, data: Vec<f32>) {
        // Acquire a write lock on the map
        let mut map = self.map.write().unwrap();
        // Attempt to get the chunk
        let chunk = map.get_mut(&[x, y, seed]);
        // If the chunk exists, append the data
        if let Some(chunk) = chunk {
            chunk.extend(data);
        } else {
            // If the chunk doesn't exist, insert it
            map.insert([x, y, seed], data);
        }
    }

    pub fn insert_chunks(&mut self, x: i32, y: i32, seed: i32, data: Vec<f32>) {
        assert!(
            data.len() % (CHUNK_SIZEU * CHUNK_SIZEU) == 0,
            "Data length is not divisible by CHUNK_SIZE squared"
        );

        let (chunk_space_x, chunk_space_y) = world_space_to_chunk_space_2d(x, y);
        let num_chunks = (HEIGHTMAP_CHUNK_SIZE / CHUNK_SIZE) as usize;
        let mut chunk_y = 0_usize;

        let mut partial_chunks: Vec<Vec<f32>> = Vec::with_capacity(num_chunks);
        partial_chunks.resize(num_chunks, Vec::new());
        // loop over the data vector
        for i in 0..HEIGHTMAP_CHUNK_SIZE as usize {
            if i != 0 && i % CHUNK_SIZEU == 0 {
                partial_chunks = Vec::with_capacity(num_chunks);
                partial_chunks.resize(num_chunks, Vec::new());
                chunk_y += 1;
            }

            for (j, item) in partial_chunks.iter_mut().enumerate().take(num_chunks) {
                let start_index = (i * HEIGHTMAP_CHUNK_SIZE as usize) + (j * CHUNK_SIZEU);
                let end_index = start_index + CHUNK_SIZEU;
                item.extend(&data[start_index..end_index]);

                if i % CHUNK_SIZEU == 15 {
                    let keyx = chunk_space_x + j as i32;
                    let keyy = chunk_space_y + chunk_y as i32;
                    self.append_chunk(keyx, keyy, seed, item.clone());
                }
            }
        }
    }

    pub fn chunks_to_generate(&self, x: i32, y: i32, seed: i32) -> Vec<[i32; 3]> {
        // x and y are in player space
        // the map contains chunks with keys in chunk space
        // we generate chunks in sets of HEIGHTMAP_CHUNK_SIZE / CHUNK_SIZE
        let mut chunks_to_generate = Vec::new();

        // Calculate chunk coordinates in chunk space with floor division in chunk space
        let (base_chunk_x, base_chunk_y) = world_space_to_heightmap_space_2d(x, y);

        let chunks_in_section = HEIGHTMAP_CHUNK_SIZE / CHUNK_SIZE;

        for dx in -1..=1 {
            for dy in -1..=1 {
                let section_x = base_chunk_x + dx;
                let section_y = base_chunk_y + dy;

                // Check a single chunk within the section to determine if the entire section needs generation
                let chunk_key = [
                    section_x * chunks_in_section,
                    section_y * chunks_in_section,
                    seed,
                ];

                // If the chunk doesn't exist in the map, mark the entire section for generation
                if !self.map.read().unwrap().contains_key(&chunk_key) {
                    // Add the section's starting coordinates in player space to the list
                    chunks_to_generate.push(chunk_key);
                    // println!("Chunk to generate: {}, {}", chunk_key[0], chunk_key[1]);
                }
            }
        }

        chunks_to_generate
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::constants::chunk::{CHUNK_SIZE, HEIGHTMAP_CHUNK_SIZE};

    fn generate_test_data(size: usize) -> Vec<f32> {
        (0..size).map(|i| i as f32).collect()
    }

    // generate chunks with 0-15 reapeating
    fn generate_test_data_identical_chunks() -> Vec<f32> {
        let mut data = Vec::new();
        let mut c = 0;
        for _ in 0..(HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) {
            data.push(c as f32);
            c += 1;
            if c >= CHUNK_SIZE {
                c = 0;
            }
        }
        data
    }

    #[test]
    fn test_insert_chunks() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        store.insert_chunks(0, 0, 0, data.clone());

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        assert!(
            store.get_chunk(0, 0, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, 63, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(63, 0, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(63, 63, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, 64, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(64, 0, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(64, 64, 0).is_none(),
            "expected a chunk to exist"
        );
    }

    #[test]
    fn test_get_chunk_non_existent() {
        let store = HeightStore::new();

        // Attempt to get a chunk that doesn't exist
        assert_eq!(store.get_chunk(0, 0, 0), None);
    }

    #[test]
    fn test_chunks_to_generate() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        let chunks_to_generate = store.chunks_to_generate(0, 0, 0);
        assert_eq!(chunks_to_generate.len(), 9, "expected 5 chunks to generate");
        store.insert_chunks(0, 0, 0, data);

        // After inserting, these chunks should be generated
        let chunks_to_generate = store.chunks_to_generate(0, 0, 0);
        assert_eq!(chunks_to_generate.len(), 8, "expected 5 chunks to generate");

        // If we check a position outside the generated chunks, it should indicate that chunks need to be generated
        let chunks_to_generate = store.chunks_to_generate(
            HEIGHTMAP_CHUNK_SIZE as i32 * 4,
            HEIGHTMAP_CHUNK_SIZE as i32,
            0,
        );
        assert_eq!(chunks_to_generate.len(), 9); // Expecting 9 sections around the point (one ahead, one behind)
    }

    #[test]
    fn test_chunks_to_generate_with_negative_coords() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        // Test with negative coordinates
        let chunks_to_generate = store.chunks_to_generate(
            -HEIGHTMAP_CHUNK_SIZE as i32,
            -HEIGHTMAP_CHUNK_SIZE as i32,
            0,
        );
        assert_eq!(chunks_to_generate.len(), 9);

        store.insert_chunks(-HEIGHTMAP_CHUNK_SIZE, -HEIGHTMAP_CHUNK_SIZE, 0, data);

        let chunks_to_generate_2 = store.chunks_to_generate(
            -(HEIGHTMAP_CHUNK_SIZE * 2) as i32,
            -HEIGHTMAP_CHUNK_SIZE as i32,
            0,
        );
        assert_eq!(chunks_to_generate_2.len(), 8);

        for [x, y, _seed] in chunks_to_generate_2 {
            assert_ne!(
                x == -64 && y == -64,
                true,
                "we should not have the -64,-64 coord"
            );
        }
    }

    #[test]
    fn test_insert_chunks_with_negative_coords() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        // Insert chunks with negative coordinates
        store.insert_chunks(
            -HEIGHTMAP_CHUNK_SIZE as i32,
            -HEIGHTMAP_CHUNK_SIZE as i32,
            0,
            data.clone(),
        );

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        assert!(
            store.get_chunk(-64, -64, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-64, -1, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-1, -64, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-1, -1, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-64, 0, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, -64, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, 0, 0).is_none(),
            "expected a chunk to exist"
        );
    }

    #[test]
    fn test_chunks_to_generate_after_full_insert() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        // Test with negative coordinates
        let chunks_to_generate = store.chunks_to_generate(0 as i32, 0 as i32, 0);
        assert_eq!(chunks_to_generate.len(), 9);

        for [x, y, seed] in chunks_to_generate {
            store.insert_chunks(x * CHUNK_SIZE, y * CHUNK_SIZE, seed, data.clone())
        }

        let chunks_to_generate = store.chunks_to_generate(0 as i32, 0 as i32, 0);
        assert_eq!(chunks_to_generate.len(), 0);
    }

    #[test]
    fn test_inserted_chunks_specific_values() {
        let mut store = HeightStore::new();
        let data = generate_test_data_identical_chunks(); // 4 chunks (2x2)

        store.insert_chunks(0, 0, 0, data.clone());

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        for x in 0..64 {
            for y in 0..64 {
                let chunk = store.get_chunk(x, y, 0).unwrap_or_default();
                for b in 0..16 {
                    assert_eq!(chunk[b], b as f32, "expected block height was wrong");
                }
            }
        }

        store.insert_chunks(
            -HEIGHTMAP_CHUNK_SIZE,
            -HEIGHTMAP_CHUNK_SIZE,
            0,
            data.clone(),
        );

        for x in -64..0 {
            for y in -64..0 {
                let chunk = store.get_chunk(x, y, 0).unwrap_or_default();
                for b in 0..16 {
                    assert_eq!(chunk[b], b as f32, "expected block height was wrong");
                }
            }
        }
    }
}
