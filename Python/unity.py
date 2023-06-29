import socket
from torch.utils.data import Dataset, DataLoader
import pytorch_lightning as pl
import struct
import json

import torch

from transforms import Transform

from torch.utils.data.sampler import SubsetRandomSampler

from torchvision.io import decode_image

class UnityDataset(Dataset):
    def __init__(self, 
                 host="127.0.0.1", 
                 port=8093, 
                 epoch_length=5000, 
                 resize=False, 
                 crop_size=False, 
                 cat=False):
        self.host = host
        self.port = port
        self.epoch_length = epoch_length
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))

        self.resize = resize
        self.crop_size = crop_size
        self.transforms = Transform(self.resize, self.crop_size)

        self.cat = cat

        self.length_bytes = 4
        self.chunk_size = 1024 

    def __del__(self):
        # Make sure to close the socket connection when the dataset is deleted
        self.socket.close()

    def __len__(self):
        # Define an arbitrary length of one "epoch"
        return self.epoch_length

    def __getitem__(self, index):
        data = {}

        # Convert index to string and send it as bytes
        index_string = str(index)
        self.socket.sendall(index_string.encode('utf-8'))

        # Define how to process each part
        processing_functions = {
            "JSON": self._receive_transform_data,
            "CAMERAS": self._receive_images,
        }

        while True:
            marker = self._receive_marker()
            if marker == b'EOT':
                break

            part_name = marker.decode('utf-8').split('_')[1]

            if part_name in processing_functions:
                data[part_name.lower()] = processing_functions[part_name]()
                _ = self._receive_marker() # END_PART

        return data

    def _receive_marker(self):
        marker_length = self._receive_length_info()
        marker = self._receive_until_length(marker_length)
        return marker

    def _receive_length_info(self):
        length_bytes = self.socket.recv(self.length_bytes)
        length = struct.unpack('!I', length_bytes)[0]
        return length

    def _receive_until_length(self, length):
        chunks = []
        while length > 0:
            chunk = self.socket.recv(min(self.chunk_size, length))
            length -= len(chunk)
            chunks.append(chunk)
        return b''.join(chunks)

    def _receive_transform_data(self):
        length = self._receive_length_info()
        json_string = self._receive_until_length(length)
        transform_dict = json.loads(json_string)
        transform_tensor = torch.hstack([torch.Tensor(list(transform_dict[key].values())) for key in transform_dict.keys()])
        return transform_tensor

    def _receive_images(self):
        images = []
        # First, receive the number of cameras
        num_cameras_data = self.socket.recv(self.length_bytes)
        num_cameras = struct.unpack('!I', num_cameras_data)[0]  # Network byte order is big endian

        for _ in range(num_cameras):
            # Receive the length of the image data
            marker = self._receive_marker()
            if marker == b'START_IMAGE':
                # Now receive the image data
                image_length = self._receive_length_info()
                received_data = self._receive_until_length(image_length)
                _ = self._receive_marker() # END_IMAGE

                image = self._process_image(received_data)

                images.append(image)
        # Apply transforms
        images = self.transforms(images)

        if self.cat:
            images = torch.cat(images, dim=0)

        return images
    
    def _process_image(self, received_data):
        # Convert data to a tensor
        tensor_data = torch.tensor(bytearray(received_data), dtype=torch.uint8)
        # Decode the image and normalize pixel values
        image = decode_image(tensor_data) / 255.0

        # Add a dimension for grayscale images
        if image.ndim == 2:
            image = image.unsqueeze(0)

        # Remove the alpha channel
        if image.shape[0] == 4:
            image = image[:3]

        return image


class UnityDataModule(pl.LightningDataModule):
    def __init__(self, dataset, batch_size=1, num_workers=0, shuffle=True, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.dataset = dataset
        self.batch_size = batch_size
        self.num_workers = num_workers
        self.shuffle = shuffle

    def train_dataloader(self):
        indices = list(range(len(self.dataset)))
        return DataLoader(self.dataset, batch_size=self.batch_size, num_workers=self.num_workers,
                          sampler=SubsetRandomSampler(indices), shuffle=False)

    def val_dataloader(self):
        indices = list(range(len(self.dataset), 2 * len(self.dataset)))
        return DataLoader(self.dataset, batch_size=self.batch_size, num_workers=self.num_workers,
                          sampler=SubsetRandomSampler(indices), shuffle=False)

    def test_dataloader(self):
        indices = list(range(2 * len(self.dataset), 3 * len(self.dataset)))
        return DataLoader(self.dataset, batch_size=self.batch_size, num_workers=self.num_workers,
                          sampler=SubsetRandomSampler(indices), shuffle=False)