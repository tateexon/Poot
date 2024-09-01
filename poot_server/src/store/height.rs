use crate::constants::chunk::{CHUNK_SIZE, CHUNK_SIZEU, HEIGHTMAP_CHUNK_SIZE};
use std::collections::HashMap;
use std::sync::{Arc, RwLock};

pub struct HeightStore {
    pub map: Arc<RwLock<HashMap<[i32; 2], Vec<f32>>>>,
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

    pub fn get_chunk(&self, x: i32, y: i32) -> Option<Vec<f32>> {
        // Acquire a read lock on the map
        let map = self.map.read().unwrap();
        // Attempt to get the chunk
        map.get(&[x, y]).cloned()
    }

    pub fn insert_chunks(&mut self, x: i32, y: i32, data: Vec<f32>) {
        // data comes in as a 1024x1024 set of f32 values placed in a 1d Vec
        // we need to break it up into CHUNK_SIZE x CHUNK_SIZE 1d Vecs and store it in the map
        // data length will always be evenly divisible by CHUNK_SIZE
        // x and y are the starting points like 0,0
        // 0,0 would be 0 throubh 15 x 0 through 15. 0, 1 would be 0 through 15 x 16 through 31 and so on
        // Ensure that data length is divisible by CHUNK_SIZE
        assert!(
            data.len() % (CHUNK_SIZEU * CHUNK_SIZEU) == 0,
            "Data length is not divisible by CHUNK_SIZE squared"
        );

        // Calculate the number of chunks per row and column
        let num_chunks = (data.len() as f32).sqrt() as usize / CHUNK_SIZEU;

        // Acquire a write lock on the map
        let mut map = self.map.write().unwrap();
        // let mut counter = 0;

        for chunk_y in 0..num_chunks {
            for chunk_x in 0..num_chunks {
                // Handle negative coordinates by applying the same floor division logic
                let cx = if (x + chunk_x as i32) < 0 {
                    (x - CHUNK_SIZE + 1) / CHUNK_SIZE + chunk_x as i32
                } else {
                    (x / CHUNK_SIZE) + chunk_x as i32
                };

                let cy = if (y + chunk_y as i32) < 0 {
                    (y - CHUNK_SIZE + 1) / CHUNK_SIZE + chunk_y as i32
                } else {
                    (y / CHUNK_SIZE) + chunk_y as i32
                };

                let key = [cx, cy];
                if map.contains_key(&key) {
                    continue;
                }

                let mut chunk = Vec::with_capacity(CHUNK_SIZEU * CHUNK_SIZEU);
                for row in 0..CHUNK_SIZEU {
                    for col in 0..CHUNK_SIZEU {
                        // Calculate the correct index in the data Vec
                        let global_x = chunk_x * CHUNK_SIZEU + col;
                        let global_y = chunk_y * CHUNK_SIZEU + row;
                        let index = global_y * num_chunks * CHUNK_SIZEU + global_x;

                        // Fill the 1D chunk with the corresponding value
                        chunk.push(data[index]);
                    }
                }

                // Insert the 1D chunk into the map with the correct key
                map.insert(key, chunk);
                // println!("Inserted chunk: {}, {}", cx, cy);
            }
        }
        // println!("Inserted {} chunks", counter);
    }

    pub fn chunks_to_generate(&self, x: i32, y: i32) -> Vec<[i32; 2]> {
        // x and y are in player space
        // the map contains chunks with keys in chunk space
        // we generate chunks in sets of HEIGHTMAP_CHUNK_SIZE / CHUNK_SIZE
        let mut chunks_to_generate = Vec::new();

        // Calculate chunk coordinates in chunk space with floor division in chunk space
        let base_chunk_x = if x < 0 {
            (x + 1) / HEIGHTMAP_CHUNK_SIZE - 1
        } else {
            x / HEIGHTMAP_CHUNK_SIZE
        };

        let base_chunk_y = if y < 0 {
            (y + 1) / HEIGHTMAP_CHUNK_SIZE - 1
        } else {
            y / HEIGHTMAP_CHUNK_SIZE
        };

        let chunks_in_section = HEIGHTMAP_CHUNK_SIZE / CHUNK_SIZE;

        for dx in -1..=1 {
            for dy in -1..=1 {
                let section_x = base_chunk_x + dx;
                let section_y = base_chunk_y + dy;

                // Check a single chunk within the section to determine if the entire section needs generation
                let chunk_key = [section_x * chunks_in_section, section_y * chunks_in_section];

                // If the chunk doesn't exist in the map, mark the entire section for generation
                if !self.map.read().unwrap().contains_key(&chunk_key) {
                    // Add the section's starting coordinates in player space to the list
                    chunks_to_generate.push(chunk_key);
                    println!("Chunk to generate: {}, {}", chunk_key[0], chunk_key[1]);
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
            if c > CHUNK_SIZE {
                c = 0;
            }
        }
        data
    }

    #[test]
    fn test_insert_chunks() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        store.insert_chunks(0, 0, data.clone());

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        assert!(store.get_chunk(0, 0).is_some(), "expected a chunk to exist");
        assert!(
            store.get_chunk(0, 63).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(63, 0).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(63, 63).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, 64).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(64, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(64, 64).is_none(),
            "expected a chunk to exist"
        );
    }

    #[test]
    fn test_get_chunk_non_existent() {
        let store = HeightStore::new();

        // Attempt to get a chunk that doesn't exist
        assert_eq!(store.get_chunk(0, 0), None);
    }

    #[test]
    fn test_chunks_to_generate() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        let chunks_to_generate = store.chunks_to_generate(0, 0);
        assert_eq!(chunks_to_generate.len(), 9, "expected 5 chunks to generate");
        store.insert_chunks(0, 0, data);

        // After inserting, these chunks should be generated
        let chunks_to_generate = store.chunks_to_generate(0, 0);
        assert_eq!(chunks_to_generate.len(), 8, "expected 5 chunks to generate");

        // If we check a position outside the generated chunks, it should indicate that chunks need to be generated
        let chunks_to_generate =
            store.chunks_to_generate(HEIGHTMAP_CHUNK_SIZE as i32 * 4, HEIGHTMAP_CHUNK_SIZE as i32);
        assert_eq!(chunks_to_generate.len(), 9); // Expecting 9 sections around the point (one ahead, one behind)
    }

    #[test]
    fn test_chunks_to_generate_with_negative_coords() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        // Test with negative coordinates
        let chunks_to_generate =
            store.chunks_to_generate(-HEIGHTMAP_CHUNK_SIZE as i32, -HEIGHTMAP_CHUNK_SIZE as i32);
        assert_eq!(chunks_to_generate.len(), 9);

        store.insert_chunks(-HEIGHTMAP_CHUNK_SIZE, -HEIGHTMAP_CHUNK_SIZE, data);

        let chunks_to_generate = store.chunks_to_generate(
            -(HEIGHTMAP_CHUNK_SIZE * 2) as i32,
            -HEIGHTMAP_CHUNK_SIZE as i32,
        );
        assert_eq!(chunks_to_generate.len(), 8);

        for [x, y] in chunks_to_generate {
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
            data.clone(),
        );

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        assert!(
            store.get_chunk(-64, -64).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-64, -1).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-1, -64).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-1, -1).is_some(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(-64, 0).is_none(),
            "expected a chunk to exist"
        );
        assert!(
            store.get_chunk(0, -64).is_none(),
            "expected a chunk to exist"
        );
        assert!(store.get_chunk(0, 0).is_none(), "expected a chunk to exist");
    }

    #[test]
    fn test_chunks_to_generate_after_full_insert() {
        let mut store = HeightStore::new();
        let data = generate_test_data((HEIGHTMAP_CHUNK_SIZE * HEIGHTMAP_CHUNK_SIZE) as usize); // 4 chunks (2x2)

        // Test with negative coordinates
        let chunks_to_generate = store.chunks_to_generate(0 as i32, 0 as i32);
        assert_eq!(chunks_to_generate.len(), 9);

        for [x, y] in chunks_to_generate {
            store.insert_chunks(x * CHUNK_SIZE, y * CHUNK_SIZE, data.clone())
        }

        let chunks_to_generate = store.chunks_to_generate(0 as i32, 0 as i32);
        assert_eq!(chunks_to_generate.len(), 0);
    }

    #[test]
    fn test_inserted_chunks_specific_values() {
        let mut store = HeightStore::new();
        let data = generate_test_data_identical_chunks(); // 4 chunks (2x2)

        store.insert_chunks(0, 0, data.clone());

        assert_eq!(store.map.read().unwrap().keys().len(), 4096);
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[0],
            0.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[1],
            1.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[2],
            2.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[3],
            3.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[4],
            4.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[5],
            5.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[6],
            6.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[7],
            7.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[8],
            8.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[9],
            9.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[10],
            10.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[11],
            11.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[12],
            12.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[13],
            13.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[14],
            14.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(0, 0).unwrap_or_default()[15],
            15.0,
            "expected block height was wrong"
        );

        store.insert_chunks(-HEIGHTMAP_CHUNK_SIZE, -HEIGHTMAP_CHUNK_SIZE, data.clone());

        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[0],
            0.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[1],
            1.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[2],
            2.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[3],
            3.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[4],
            4.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[5],
            5.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[6],
            6.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[7],
            7.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[8],
            8.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[9],
            9.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[10],
            10.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[11],
            11.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[12],
            12.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[13],
            13.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[14],
            14.0,
            "expected block height was wrong"
        );
        assert_eq!(
            store.get_chunk(-64, -64).unwrap_or_default()[15],
            15.0,
            "expected block height was wrong"
        );
    }
}
