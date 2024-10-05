pub fn get_2d_index(x: usize, y: usize, width: usize) -> usize {
    x + (y * width)
}

pub fn get_2d_coordinates(index: usize, width: usize) -> (usize, usize) {
    let y = index % width;
    let x = index / width;
    (x, y)
}

pub fn get_2d_value<T: Copy>(vec: &[T], x: usize, y: usize, width: usize) -> Option<T> {
    let index = get_2d_index(x, y, width);
    vec.get(index).copied()
}

pub fn insert_2d_value<T: Copy>(
    vec: &mut [T],
    x: usize,
    y: usize,
    width: usize,
    value: T,
) -> Result<(), &'static str> {
    let index = get_2d_index(x, y, width);
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
    fn test_get_2d_index() {
        assert_eq!(get_2d_index(0, 0, 10), 0);
        assert_eq!(get_2d_index(1, 0, 10), 1);
        assert_eq!(get_2d_index(0, 1, 10), 10);
        assert_eq!(get_2d_index(5, 3, 10), 35);
    }

    #[test]
    fn test_get_2d_value() {
        let width = 10;
        let height = 10;
        let mut data = vec![0.0; width * height];
        data[35] = 5.5;

        assert_eq!(get_2d_value(&data, 5, 3, width), Some(5.5));
        assert_eq!(get_2d_value(&data, 0, 0, width), Some(0.0));
        assert_eq!(get_2d_value(&data, 9, 9, width), Some(0.0));
        assert_eq!(get_2d_value(&data, 10, 10, width), None); // Out of bounds
    }

    #[test]
    fn test_insert_2d_value() {
        let width = 10;
        let height = 10;
        let mut data = vec![0.0; width * height];

        assert!(insert_2d_value(&mut data, 5, 3, width, 5.5).is_ok());
        assert_eq!(data[35], 5.5);

        assert!(insert_2d_value(&mut data, 10, 10, width, 2.2).is_err()); // Out of bounds
    }

    #[test]
    fn test_generic_get_2d_value() {
        let width = 10;
        let mut data_f64 = vec![0.0f64; width * width];
        let mut data_f32 = vec![0.0f32; width * width];
        let mut data_i32 = vec![0i32; width * width];

        insert_2d_value(&mut data_f64, 2, 3, width, 1.23f64).unwrap();
        insert_2d_value(&mut data_f32, 2, 3, width, 1.23f32).unwrap();
        insert_2d_value(&mut data_i32, 2, 3, width, 42i32).unwrap();

        assert_eq!(get_2d_value(&data_f64, 2, 3, width), Some(1.23f64));
        assert_eq!(get_2d_value(&data_f32, 2, 3, width), Some(1.23f32));
        assert_eq!(get_2d_value(&data_i32, 2, 3, width), Some(42i32));
    }
}
