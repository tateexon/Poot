use super::hardware::Hardware;
use crate::constants::chunk::HEIGHTMAP_CHUNK_SIZE;
use ash::vk::{self, MemoryAllocateInfo};

pub struct Buffer {
    pub buffer: vk::Buffer,
    pub memory: vk::DeviceMemory,
}

#[repr(C)]
#[derive(Copy, Clone)]
struct UniformBufferObject {
    coordinates: [f32; 2],
    seed: f32,
}

impl Buffer {
    // Create Uniform Buffer
    pub fn create_uniform_buffer(d: &Hardware, x_coord: f32, y_coord: f32, seed: f32) -> Buffer {
        let ubo = UniformBufferObject {
            coordinates: [x_coord, y_coord],
            seed,
        };
        let ubo_size = std::mem::size_of::<UniformBufferObject>() as vk::DeviceSize;

        let non_coherent_atom_size = d.physical_device_properties.limits.non_coherent_atom_size;
        let aligned_ubo_size = ((ubo_size + non_coherent_atom_size - 1) / non_coherent_atom_size)
            * non_coherent_atom_size;
        let (ubo_buffer, mem_alloc_info) = Self::get_buffer_mem_alloc_info(
            d,
            vk::BufferUsageFlags::UNIFORM_BUFFER,
            aligned_ubo_size,
        );

        // push buffer to device

        let ubo_buffer_memory = unsafe {
            d.device
                .allocate_memory(&mem_alloc_info, None)
                .expect("Failed to allocate UBO buffer memory")
        };

        unsafe {
            d.device
                .bind_buffer_memory(ubo_buffer, ubo_buffer_memory, 0)
                .expect("Failed to bind UBO buffer memory");

            // Map and copy data
            let data_ptr =
                d.device
                    .map_memory(
                        ubo_buffer_memory,
                        0,
                        aligned_ubo_size,
                        vk::MemoryMapFlags::empty(),
                    )
                    .expect("Failed to map UBO memory") as *mut UniformBufferObject;

            data_ptr.write(ubo);

            d.device
                .flush_mapped_memory_ranges(&[vk::MappedMemoryRange::default()
                    .memory(ubo_buffer_memory)
                    .offset(0)
                    .size(aligned_ubo_size)])
                .expect("Failed to flush UBO memory");

            d.device.unmap_memory(ubo_buffer_memory);
        }

        Buffer {
            buffer: ubo_buffer,
            memory: ubo_buffer_memory,
        }
    }

    pub fn create_storage_buffer(device: &Hardware) -> Buffer {
        let buffer_size = (HEIGHTMAP_CHUNK_SIZE as usize
            * HEIGHTMAP_CHUNK_SIZE as usize
            * std::mem::size_of::<f32>()) as vk::DeviceSize;

        let (storage_buffer, mem_alloc_info) = Self::get_buffer_mem_alloc_info(
            device,
            vk::BufferUsageFlags::STORAGE_BUFFER | vk::BufferUsageFlags::TRANSFER_DST,
            buffer_size,
        );

        // Bind to device

        let storage_buffer_memory = unsafe {
            device
                .device
                .allocate_memory(&mem_alloc_info, None)
                .expect("Failed to allocate storage buffer memory")
        };

        unsafe {
            device
                .device
                .bind_buffer_memory(storage_buffer, storage_buffer_memory, 0)
                .expect("Failed to bind storage buffer memory")
        };

        Buffer {
            buffer: storage_buffer,
            memory: storage_buffer_memory,
        }
    }

    pub fn find_memory_type(
        memory_type_bits: u32,
        properties: vk::MemoryPropertyFlags,
        memory_properties: vk::PhysicalDeviceMemoryProperties,
    ) -> u32 {
        for (i, memory_type) in memory_properties.memory_types.iter().enumerate() {
            if (memory_type_bits & (1 << i)) != 0
                && (memory_type.property_flags & properties) == properties
            {
                return i as u32;
            }
        }
        panic!("Failed to find suitable memory type!");
    }

    pub fn find_memorytype_index(
        memory_req: &vk::MemoryRequirements,
        memory_prop: &vk::PhysicalDeviceMemoryProperties,
        flags: vk::MemoryPropertyFlags,
    ) -> Option<u32> {
        memory_prop.memory_types[..memory_prop.memory_type_count as _]
            .iter()
            .enumerate()
            .find(|(index, memory_type)| {
                (1 << index) & memory_req.memory_type_bits != 0
                    && memory_type.property_flags & flags == flags
            })
            .map(|(index, _memory_type)| index as _)
    }

    fn get_buffer_mem_alloc_info(
        d: &Hardware,
        usage_flags: vk::BufferUsageFlags,
        buffer_size: u64,
    ) -> (vk::Buffer, vk::MemoryAllocateInfo) {
        let buffer_create_info = vk::BufferCreateInfo::default()
            .size(buffer_size)
            .usage(usage_flags)
            .sharing_mode(vk::SharingMode::EXCLUSIVE);
        let ubo_buffer = unsafe {
            d.device
                .create_buffer(&buffer_create_info, None)
                .expect("Failed to create UBO buffer")
        };

        let mem_requirements = unsafe { d.device.get_buffer_memory_requirements(ubo_buffer) };
        let memory_type = Self::find_memory_type(
            mem_requirements.memory_type_bits,
            vk::MemoryPropertyFlags::HOST_VISIBLE | vk::MemoryPropertyFlags::HOST_COHERENT,
            d.physical_device_memory_properties,
        );

        let mem_alloc_info: MemoryAllocateInfo = vk::MemoryAllocateInfo::default()
            .allocation_size(mem_requirements.size)
            .memory_type_index(memory_type);

        (ubo_buffer, mem_alloc_info)
    }
}
