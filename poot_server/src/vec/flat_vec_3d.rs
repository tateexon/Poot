pub fn get_3d_index(x: usize, y: usize, z: usize, width: usize, height: usize) -> usize {
    x + y * width + z * width * height
}

pub fn get_3d_value<T: Copy>(
    vec: &[T],
    x: usize,
    y: usize,
    z: usize,
    width: usize,
    height: usize,
) -> Option<T> {
    let index = get_3d_index(x, y, z, width, height);
    vec.get(index).copied()
}

pub fn insert_3d_value<T: Copy>(
    vec: &mut [T],
    x: usize,
    y: usize,
    z: usize,
    width: usize,
    height: usize,
    value: T,
) -> Result<(), &'static str> {
    let index = get_3d_index(x, y, z, width, height);
    if index < vec.len() {
        vec[index] = value;
        Ok(())
    } else {
        Err("Index out of bounds")
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_get_3d_index() {
        assert_eq!(get_3d_index(0, 0, 0, 10, 10), 0);
        assert_eq!(get_3d_index(1, 0, 0, 10, 10), 1);
        assert_eq!(get_3d_index(0, 1, 0, 10, 10), 10);
        assert_eq!(get_3d_index(0, 0, 1, 10, 10), 100);
        assert_eq!(get_3d_index(5, 3, 2, 10, 10), 235);
    }

    #[test]
    fn test_get_3d_value() {
        let width = 10;
        let height = 10;
        let depth = 10;
        let mut data = vec![0.0; width * height * depth];
        data[235] = 7.7;

        assert_eq!(get_3d_value(&data, 5, 3, 2, width, height), Some(7.7));
        assert_eq!(get_3d_value(&data, 0, 0, 0, width, height), Some(0.0));
        assert_eq!(get_3d_value(&data, 9, 9, 9, width, height), Some(0.0));
        assert_eq!(get_3d_value(&data, 10, 10, 10, width, height), None); // Out of bounds
    }

    #[test]
    fn test_insert_3d_value() {
        let width = 10;
        let height = 10;
        let depth = 10;
        let mut data = vec![0.0; width * height * depth];

        assert!(insert_3d_value(&mut data, 5, 3, 2, width, height, 7.7).is_ok());
        assert_eq!(data[235], 7.7);

        assert!(insert_3d_value(&mut data, 10, 10, 10, width, height, 3.3).is_err());
        // Out of bounds
    }

    #[test]
    fn test_generic_get_3d_value() {
        let width = 10;
        let height = 10;
        let depth = 10;

        let mut data_f64 = vec![0.0f64; width * height * depth];
        let mut data_f32 = vec![0.0f32; width * height * depth];
        let mut data_i32 = vec![0i32; width * height * depth];

        insert_3d_value(&mut data_f64, 2, 3, 4, width, height, 7.7f64).unwrap();
        insert_3d_value(&mut data_f32, 2, 3, 4, width, height, 7.7f32).unwrap();
        insert_3d_value(&mut data_i32, 2, 3, 4, width, height, 77i32).unwrap();

        assert_eq!(
            get_3d_value(&data_f64, 2, 3, 4, width, height),
            Some(7.7f64)
        );
        assert_eq!(
            get_3d_value(&data_f32, 2, 3, 4, width, height),
            Some(7.7f32)
        );
        assert_eq!(get_3d_value(&data_i32, 2, 3, 4, width, height), Some(77i32));
    }
}
