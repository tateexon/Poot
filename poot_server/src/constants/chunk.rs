pub const CHUNK_SIZE: i32 = 16;
pub const CHUNK_SIZEU: usize = CHUNK_SIZE as usize;
pub const CHUNK_SIZEF: f64 = CHUNK_SIZE as f64;

pub const HEIGHTMAP_CHUNK_SIZE: i32 = 1024;
pub const HEIGHTMAP_CHUNK_SIZEF: f64 = HEIGHTMAP_CHUNK_SIZE as f64;

pub fn chunk_space_to_world_space_2d(x: i32, y: i32) -> (i32, i32) {
    (x * CHUNK_SIZE, y * CHUNK_SIZE)
}

pub fn world_space_to_chunk_space_2d(x: i32, y: i32) -> (i32, i32) {
    (
        (x as f64 / CHUNK_SIZEF).floor() as i32,
        (y as f64 / CHUNK_SIZEF).floor() as i32,
    )
}

pub fn world_space_to_heightmap_space_2d(x: i32, y: i32) -> (i32, i32) {
    (
        (x as f64 / HEIGHTMAP_CHUNK_SIZEF).floor() as i32,
        (y as f64 / HEIGHTMAP_CHUNK_SIZEF).floor() as i32,
    )
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_world_space_to_chunk_space_2d() {
        let (x, y) = world_space_to_chunk_space_2d(0, 0);
        assert_eq!(x, 0);
        assert_eq!(y, 0);
        let (x, y) = world_space_to_chunk_space_2d(15, 15);
        assert_eq!(x, 0);
        assert_eq!(y, 0);
        let (x, y) = world_space_to_chunk_space_2d(16, 16);
        assert_eq!(x, 1);
        assert_eq!(y, 1);
        let (x, y) = world_space_to_chunk_space_2d(-1, -1);
        assert_eq!(x, -1);
        assert_eq!(y, -1);
        let (x, y) = world_space_to_chunk_space_2d(-16, -16);
        assert_eq!(x, -1);
        assert_eq!(y, -1);
        let (x, y) = world_space_to_chunk_space_2d(-17, -17);
        assert_eq!(x, -2);
        assert_eq!(y, -2);
    }

    #[test]
    fn test_world_space_to_heightmap_space_2d() {
        let (x, y) = world_space_to_heightmap_space_2d(0, 0);
        assert_eq!(x, 0);
        assert_eq!(y, 0);
        let (x, y) = world_space_to_heightmap_space_2d(1023, 1023);
        assert_eq!(x, 0);
        assert_eq!(y, 0);
        let (x, y) = world_space_to_heightmap_space_2d(1024, 1024);
        assert_eq!(x, 1);
        assert_eq!(y, 1);
        let (x, y) = world_space_to_heightmap_space_2d(-1, -1);
        assert_eq!(x, -1);
        assert_eq!(y, -1);
        let (x, y) = world_space_to_heightmap_space_2d(-1024, -1024);
        assert_eq!(x, -1);
        assert_eq!(y, -1);
        let (x, y) = world_space_to_heightmap_space_2d(-1025, -1025);
        assert_eq!(x, -2);
        assert_eq!(y, -2);
    }

    #[test]
    fn test_chunk_space_to_world_space_2d() {
        assert_eq!(chunk_space_to_world_space_2d(0, 0), (0, 0));
        assert_eq!(chunk_space_to_world_space_2d(1, 1), (16, 16));
        assert_eq!(chunk_space_to_world_space_2d(-1, -1), (-16, -16));
        assert_eq!(chunk_space_to_world_space_2d(-2, -2), (-32, -32));
    }
}
