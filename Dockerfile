# Use Unity editor image
FROM unityci/editor:ubuntu-2022.3.51f1-android-3

# Install dependencies
RUN apt-get update && apt-get install -y \
    unzip zip libvulkan1 libx11-dev libxi-dev libxcursor-dev libxrandr-dev libxinerama-dev libgl1-mesa-dev \
    dos2unix \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /project

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
RUN dos2unix /entrypoint.sh

# Default command
ENTRYPOINT ["/entrypoint.sh"]