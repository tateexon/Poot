use ash::vk;
use std::ffi::CString;

pub struct Hardware {
    #[allow(dead_code)]
    entry: ash::Entry,
    pub device: ash::Device,
    pub physical_device: vk::PhysicalDevice,
    pub physical_device_properties: vk::PhysicalDeviceProperties,
    pub physical_device_memory_properties: vk::PhysicalDeviceMemoryProperties,

    pub queue: vk::Queue,
    pub queue_family_index: u32,

    pub instance: ash::Instance,
}

impl Default for Hardware {
    fn default() -> Self {
        Self::new()
    }
}

impl Hardware {
    pub fn new() -> Self {
        let entry = Self::create_entry();
        let instance = Self::create_instance(&entry);

        //Select Physical Device
        let physical_device = Self::pick_physical_device(&instance);

        //Create Logical Device and Queue
        let (device, queue, queue_family_index) =
            Self::create_logical_device(&instance, physical_device);

        let physical_device_properties =
            unsafe { instance.get_physical_device_properties(physical_device) };
        let physical_device_memory_properties =
            unsafe { instance.get_physical_device_memory_properties(physical_device) };

        Self {
            entry,
            device,
            physical_device,
            physical_device_properties,
            physical_device_memory_properties,
            queue,
            queue_family_index,
            instance,
        }
    }

    // Initialize Vulkan Entry
    pub fn create_entry() -> ash::Entry {
        unsafe { ash::Entry::load().expect("Failed to load Vulkan entry") }
    }

    // Create Vulkan Instance
    pub fn create_instance(entry: &ash::Entry) -> ash::Instance {
        let app_name = CString::new("Vulkan App").expect("CString::new failed");
        let layer_names = [CString::new("VK_LAYER_KHRONOS_validation").unwrap()];
        let layers: Vec<*const i8> = layer_names.iter().map(|layer| layer.as_ptr()).collect();

        let app_info = vk::ApplicationInfo::default()
            .application_name(&app_name)
            .application_version(0)
            .engine_name(&app_name)
            .engine_version(0)
            .api_version(vk::make_api_version(0, 1, 0, 0));

        let create_info = vk::InstanceCreateInfo::default()
            .application_info(&app_info)
            .enabled_layer_names(&layers);

        unsafe {
            entry
                .create_instance(&create_info, None)
                .expect("Instance creation error")
        }
    }

    // Select Physical Device
    fn pick_physical_device(instance: &ash::Instance) -> vk::PhysicalDevice {
        unsafe {
            instance
                .enumerate_physical_devices()
                .expect("Failed to enumerate physical devices")
                .into_iter()
                .next()
                .expect("No physical devices found")
        }
    }

    // Create Logical Device and Retrieve Queue
    fn create_logical_device(
        instance: &ash::Instance,
        physical_device: vk::PhysicalDevice,
    ) -> (ash::Device, vk::Queue, u32) {
        // Find queue family index
        let queue_family_index = Self::find_queue_family(instance, physical_device);

        let queue_priorities = [1.0f32];
        let device_queue_create_info = vk::DeviceQueueCreateInfo::default()
            .queue_family_index(queue_family_index)
            .queue_priorities(&queue_priorities);

        let device_create_info = vk::DeviceCreateInfo::default()
            .queue_create_infos(std::slice::from_ref(&device_queue_create_info));

        let device: ash::Device = unsafe {
            instance
                .create_device(physical_device, &device_create_info, None)
                .expect("Failed to create logical device")
        };

        let queue = unsafe { device.get_device_queue(queue_family_index, 0) };

        (device, queue, queue_family_index)
    }

    // Helper to find suitable queue family
    fn find_queue_family(instance: &ash::Instance, physical_device: vk::PhysicalDevice) -> u32 {
        unsafe {
            instance
                .get_physical_device_queue_family_properties(physical_device)
                .iter()
                .enumerate()
                .find_map(|(index, info)| {
                    if info.queue_flags.contains(vk::QueueFlags::COMPUTE) {
                        Some(index as u32)
                    } else {
                        None
                    }
                })
                .expect("No suitable queue family found")
        }
    }
}
