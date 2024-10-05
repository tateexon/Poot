use crate::{
    constants::chunk::HEIGHTMAP_CHUNK_SIZE,
    vulkan::{buffer::Buffer, hardware::Hardware},
};
use ash::util::*;
use ash::vk;
use std::ffi::CString;
use std::io::Cursor;
use std::slice;

pub struct ComputeShader<'a> {
    hardware: &'a Hardware,
    ubo_buf: Buffer,
    heightmap_buf: Buffer,

    compute_pipeline: vk::Pipeline,
    pipeline_layout: vk::PipelineLayout,
    descriptor_set_layout: vk::DescriptorSetLayout,
    descriptor_pool: vk::DescriptorPool,
    command_pool: vk::CommandPool,
    pub heightmap_data: Vec<f32>,
}

impl<'a> Drop for ComputeShader<'a> {
    fn drop(&mut self) {
        self.cleanup(&self.hardware.device);
    }
}

impl<'a> ComputeShader<'a> {
    fn cleanup(&self, device: &ash::Device) {
        unsafe {
            // Destroy buffers and free memory
            device.destroy_buffer(self.ubo_buf.buffer, None);
            device.free_memory(self.ubo_buf.memory, None);
            device.destroy_buffer(self.heightmap_buf.buffer, None);
            device.free_memory(self.heightmap_buf.memory, None);

            // actually we want to hold onto this until we are done using it.
            // Destroy shader module
            // device.destroy_shader_module(self.shader_module, None);

            // Destroy pipeline
            device.destroy_pipeline(self.compute_pipeline, None);
            device.destroy_pipeline_layout(self.pipeline_layout, None);

            // Destroy descriptor set layout and pool
            device.destroy_descriptor_set_layout(self.descriptor_set_layout, None);
            device.destroy_descriptor_pool(self.descriptor_pool, None);

            // Destroy command pool
            device.destroy_command_pool(self.command_pool, None);

            // Finally, destroy the logical device
            // Actually we can't do this, it causes errors
            // device.destroy_device(None);

            // Instance destruction happens outside since it’s usually created and managed at a higher level
            // instance.destroy_instance(None);
        }
    }

    pub fn to_string(slice: Vec<f32>) -> String {
        slice
            .iter()
            .map(|x| format!("{:.6}", x)) // Convert each f32 to a string
            .collect::<Vec<String>>() // Collect into a vector of strings
            .join(" ") // Join with a space separator
    }

    // 5. Create Shader Module
    pub fn create_shader_module(device: &ash::Device) -> vk::ShaderModule {
        let mut spv_file = Cursor::new(&include_bytes!("../../shaders/noise.spv")[..]);
        let spv_code = read_spv(&mut spv_file).expect("Failed to read shader spv file");
        let shader_module_create_info =
            vk::ShaderModuleCreateInfo::default().code(bytemuck::cast_slice(&spv_code));

        unsafe {
            device
                .create_shader_module(&shader_module_create_info, None)
                .expect("Shader module creation error")
        }
    }

    // 8. Create Descriptor Set Layout
    fn create_descriptor_set_layout(device: &ash::Device) -> vk::DescriptorSetLayout {
        let bindings = [
            vk::DescriptorSetLayoutBinding {
                binding: 0,
                descriptor_type: vk::DescriptorType::UNIFORM_BUFFER,
                descriptor_count: 1,
                stage_flags: vk::ShaderStageFlags::COMPUTE,
                ..Default::default()
            },
            vk::DescriptorSetLayoutBinding {
                binding: 1,
                descriptor_type: vk::DescriptorType::STORAGE_BUFFER,
                descriptor_count: 1,
                stage_flags: vk::ShaderStageFlags::COMPUTE,
                ..Default::default()
            },
        ];

        let layout_create_info = vk::DescriptorSetLayoutCreateInfo::default().bindings(&bindings);

        unsafe {
            device
                .create_descriptor_set_layout(&layout_create_info, None)
                .expect("Failed to create descriptor set layout")
        }
    }

    // 9. Create Pipeline Layout
    fn create_pipeline_layout(
        device: &ash::Device,
        descriptor_set_layout: &vk::DescriptorSetLayout,
    ) -> vk::PipelineLayout {
        let pipeline_layout_create_info = vk::PipelineLayoutCreateInfo::default()
            .set_layouts(std::slice::from_ref(descriptor_set_layout));

        unsafe {
            device
                .create_pipeline_layout(&pipeline_layout_create_info, None)
                .expect("Failed to create pipeline layout")
        }
    }

    // 10. Create Compute Pipeline
    fn create_compute_pipeline(
        device: &ash::Device,
        shader_module: vk::ShaderModule,
        pipeline_layout: vk::PipelineLayout,
    ) -> vk::Pipeline {
        let binding = CString::new("main").unwrap();
        let shader_stage_create_info = vk::PipelineShaderStageCreateInfo::default()
            .module(shader_module)
            .name(binding.as_c_str())
            .stage(vk::ShaderStageFlags::COMPUTE);

        let pipeline_create_info = vk::ComputePipelineCreateInfo::default()
            .stage(shader_stage_create_info)
            .layout(pipeline_layout);

        unsafe {
            device
                .create_compute_pipelines(
                    vk::PipelineCache::null(),
                    std::slice::from_ref(&pipeline_create_info),
                    None,
                )
                .expect("Failed to create compute pipeline")[0]
        }
    }

    // 11. Create Descriptor Pool
    fn create_descriptor_pool(device: &ash::Device) -> vk::DescriptorPool {
        let pool_sizes = [
            vk::DescriptorPoolSize {
                ty: vk::DescriptorType::UNIFORM_BUFFER,
                descriptor_count: 1,
            },
            vk::DescriptorPoolSize {
                ty: vk::DescriptorType::STORAGE_BUFFER,
                descriptor_count: 1,
            },
        ];

        let pool_create_info = vk::DescriptorPoolCreateInfo::default()
            .pool_sizes(&pool_sizes)
            .max_sets(1);

        unsafe {
            device
                .create_descriptor_pool(&pool_create_info, None)
                .expect("Failed to create descriptor pool")
        }
    }

    // 12. Allocate Descriptor Set
    fn allocate_descriptor_set(
        device: &ash::Device,
        descriptor_pool: vk::DescriptorPool,
        descriptor_set_layout: &vk::DescriptorSetLayout,
    ) -> Vec<vk::DescriptorSet> {
        let allocate_info = vk::DescriptorSetAllocateInfo::default()
            .descriptor_pool(descriptor_pool)
            .set_layouts(std::slice::from_ref(descriptor_set_layout));

        unsafe {
            device
                .allocate_descriptor_sets(&allocate_info)
                .expect("Failed to allocate descriptor set")
        }
    }

    // 13. Update Descriptor Set with Buffers
    fn update_descriptor_set(
        device: &ash::Device,
        descriptor_sets: Vec<vk::DescriptorSet>,
        ubo_buffer: vk::Buffer,
        storage_buffer: vk::Buffer,
    ) {
        let ubo_buffer_info = vk::DescriptorBufferInfo::default()
            .buffer(ubo_buffer)
            .offset(0)
            .range(vk::WHOLE_SIZE);

        let storage_buffer_info = vk::DescriptorBufferInfo::default()
            .buffer(storage_buffer)
            .offset(0)
            .range(vk::WHOLE_SIZE);

        let descriptor_writes = [
            vk::WriteDescriptorSet {
                dst_set: descriptor_sets[0],
                descriptor_count: 1,
                dst_binding: 0,
                descriptor_type: vk::DescriptorType::UNIFORM_BUFFER,
                p_buffer_info: &ubo_buffer_info,
                ..Default::default()
            },
            vk::WriteDescriptorSet {
                dst_set: descriptor_sets[0],
                descriptor_count: 1,
                dst_binding: 1,
                descriptor_type: vk::DescriptorType::STORAGE_BUFFER,
                p_buffer_info: &storage_buffer_info,
                ..Default::default()
            },
        ];

        unsafe {
            device.update_descriptor_sets(&descriptor_writes, &[]);
        }
    }

    // 14. Create Command Pool
    fn create_command_pool(device: &ash::Device, queue_family_index: u32) -> vk::CommandPool {
        let pool_create_info = vk::CommandPoolCreateInfo::default()
            .queue_family_index(queue_family_index)
            .flags(vk::CommandPoolCreateFlags::RESET_COMMAND_BUFFER);
        unsafe {
            device
                .create_command_pool(&pool_create_info, None)
                .expect("Failed to create command pool")
        }
    }

    // 15. Create Command Buffer
    fn create_command_buffer(
        device: &ash::Device,
        command_pool: vk::CommandPool,
    ) -> vk::CommandBuffer {
        let allocate_info = vk::CommandBufferAllocateInfo::default()
            .command_pool(command_pool)
            .level(vk::CommandBufferLevel::PRIMARY)
            .command_buffer_count(1);
        unsafe {
            device
                .allocate_command_buffers(&allocate_info)
                .expect("Failed to allocate command buffers")[0]
        }
    }

    // 16. Record Command Buffer
    fn record_command_buffer(
        device: &ash::Device,
        command_buffer: vk::CommandBuffer,
        compute_pipeline: vk::Pipeline,
        pipeline_layout: vk::PipelineLayout,
        descriptor_sets: Vec<vk::DescriptorSet>,
    ) {
        let begin_info = vk::CommandBufferBeginInfo::default();

        unsafe {
            device
                .begin_command_buffer(command_buffer, &begin_info)
                .expect("Failed to begin command buffer");

            device.cmd_bind_descriptor_sets(
                command_buffer,
                vk::PipelineBindPoint::COMPUTE,
                pipeline_layout,
                0,
                &descriptor_sets[..],
                &[],
            );

            device.cmd_bind_pipeline(
                command_buffer,
                vk::PipelineBindPoint::COMPUTE,
                compute_pipeline,
            );

            device.cmd_dispatch(
                command_buffer,
                (HEIGHTMAP_CHUNK_SIZE / 16) as u32,
                (HEIGHTMAP_CHUNK_SIZE / 16) as u32,
                1,
            );

            device
                .end_command_buffer(command_buffer)
                .expect("Failed to end command buffer");
        }
    }

    // 17. Submit Command Buffer
    fn submit_command_buffer(
        device: &ash::Device,
        queue: vk::Queue,
        command_buffer: vk::CommandBuffer,
    ) {
        let submit_info =
            vk::SubmitInfo::default().command_buffers(std::slice::from_ref(&command_buffer));

        unsafe {
            device
                .queue_submit(queue, &[submit_info], vk::Fence::null())
                .expect("Failed to submit queue");
            device
                .queue_wait_idle(queue)
                .expect("Failed to wait for queue idle");
        }
    }

    // 18. Retrieve Data from Storage Buffer
    fn retrieve_buffer_data(
        device: &ash::Device,
        buffer_memory: vk::DeviceMemory,
        size: usize,
    ) -> Vec<f32> {
        unsafe {
            let data_ptr = device
                .map_memory(
                    buffer_memory,
                    0,
                    size as vk::DeviceSize,
                    vk::MemoryMapFlags::empty(),
                )
                .expect("Failed to map memory") as *const f32;

            let data = slice::from_raw_parts(data_ptr, size).to_vec();

            device.unmap_memory(buffer_memory);

            data
        }
    }

    pub fn shader_compute(
        hardware: &Hardware,
        shader: vk::ShaderModule,
        x_coord: f32,
        y_coord: f32,
        seed: f32,
    ) -> ComputeShader {
        // 5. Create Buffers
        let ubo_buf: Buffer = Buffer::create_uniform_buffer(hardware, x_coord, y_coord, seed);
        let heightmap_buf: Buffer = Buffer::create_storage_buffer(hardware);

        // 6. Create Descriptor Set Layout and Pipeline Layout
        let descriptor_set_layout = Self::create_descriptor_set_layout(&hardware.device);

        // 8. Create Descriptor Pool and Allocate Descriptor Set
        let descriptor_pool = Self::create_descriptor_pool(&hardware.device);
        let descriptor_sets = Self::allocate_descriptor_set(
            &hardware.device,
            descriptor_pool,
            &descriptor_set_layout,
        );

        // 9. Update Descriptor Set with Buffers
        Self::update_descriptor_set(
            &hardware.device,
            descriptor_sets.clone(),
            ubo_buf.buffer,
            heightmap_buf.buffer,
        );

        // 7. Create Compute Pipeline
        let pipeline_layout =
            Self::create_pipeline_layout(&hardware.device, &descriptor_set_layout);
        let compute_pipeline =
            Self::create_compute_pipeline(&hardware.device, shader, pipeline_layout);

        // 10. Create Command Pool and Command Buffer
        let command_pool = Self::create_command_pool(&hardware.device, hardware.queue_family_index);
        let command_buffer = Self::create_command_buffer(&hardware.device, command_pool);

        // 11. Record Command Buffer
        Self::record_command_buffer(
            &hardware.device,
            command_buffer,
            compute_pipeline,
            pipeline_layout,
            descriptor_sets,
        );

        // 12. Submit Command Buffer and Wait
        Self::submit_command_buffer(&hardware.device, hardware.queue, command_buffer);

        // 13. Retrieve Data from Storage Buffer
        let heightmap_data = Self::retrieve_buffer_data(
            &hardware.device,
            heightmap_buf.memory,
            HEIGHTMAP_CHUNK_SIZE as usize * HEIGHTMAP_CHUNK_SIZE as usize,
        );

        ComputeShader {
            hardware,
            ubo_buf,
            heightmap_buf,
            compute_pipeline,
            pipeline_layout,
            descriptor_set_layout,
            descriptor_pool,
            command_pool,
            heightmap_data,
        }
    }
}
