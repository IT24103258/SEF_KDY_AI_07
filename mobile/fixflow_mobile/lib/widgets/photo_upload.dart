import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../core/theme/app_colors.dart';
import 'photo_thumbnail.dart';

class PhotoItem {
  final String id;
  final String name;
  final Uint8List bytes;
  bool isUploading;
  bool isUploaded;
  String? fileKey;

  PhotoItem({
    required this.id,
    required this.name,
    required this.bytes,
    this.isUploading = false,
    this.isUploaded = false,
    this.fileKey,
  });
}

class PhotoUpload extends StatefulWidget {
  final List<PhotoItem> photos;
  final ValueChanged<PhotoItem>? onAddPhoto;
  final ValueChanged<String>? onRemovePhoto;
  final Future<String?> Function(PhotoItem item)? onUploadPhoto;

  const PhotoUpload({
    super.key,
    required this.photos,
    this.onAddPhoto,
    this.onRemovePhoto,
    this.onUploadPhoto,
  });

  @override
  State<PhotoUpload> createState() => PhotoUploadState();
}

class PhotoUploadState extends State<PhotoUpload> {
  final ImagePicker _imagePicker = ImagePicker();

  void showImageSourceDialog() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                leading: const CircleAvatar(
                  backgroundColor: AppColors.primaryAccent,
                  child:
                      Icon(Icons.camera_alt_outlined, color: AppColors.primary),
                ),
                title: const Text('Take photo'),
                subtitle: const Text('Capture repair or completion evidence'),
                onTap: () {
                  Navigator.pop(context);
                  _pickPhoto(ImageSource.camera);
                },
              ),
              const Divider(height: 1),
              ListTile(
                leading: const CircleAvatar(
                  backgroundColor: AppColors.primaryAccent,
                  child: Icon(Icons.photo_library_outlined,
                      color: AppColors.primary),
                ),
                title: const Text('Choose from gallery'),
                subtitle: const Text('Select existing completion evidence'),
                onTap: () {
                  Navigator.pop(context);
                  _pickPhoto(ImageSource.gallery);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _pickPhoto(ImageSource source) async {
    try {
      final image = await _imagePicker.pickImage(source: source);
      if (image == null) return;

      final bytes = await image.readAsBytes();
      if (bytes.isEmpty) return;

      final item = PhotoItem(
        id: DateTime.now().microsecondsSinceEpoch.toString(),
        name: image.name,
        bytes: bytes,
      );
      widget.onAddPhoto?.call(item);

      if (widget.onUploadPhoto == null) return;
      if (mounted) setState(() => item.isUploading = true);
      final fileKey = await widget.onUploadPhoto!(item);
      if (!mounted) return;
      setState(() {
        item.isUploading = false;
        item.isUploaded = fileKey != null;
        item.fileKey = fileKey;
      });
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Unable to select this photo. Please try again.')),
      );
    }
  }

  Future<void> _retryUpload(PhotoItem photo) async {
    if (widget.onUploadPhoto == null) return;
    setState(() => photo.isUploading = true);
    final fileKey = await widget.onUploadPhoto!(photo);
    if (!mounted) return;
    setState(() {
      photo.isUploading = false;
      photo.isUploaded = fileKey != null;
      photo.fileKey = fileKey;
    });
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              'Photos (${widget.photos.length})',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: isDark
                    ? AppColors.darkTextPrimary
                    : AppColors.lightTextPrimary,
              ),
            ),
            TextButton.icon(
              onPressed: showImageSourceDialog,
              icon: const Icon(Icons.add_a_photo_outlined, size: 16),
              label: const Text('Add photo', style: TextStyle(fontSize: 12)),
            ),
          ],
        ),
        const SizedBox(height: 8),
        if (widget.photos.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: isDark ? AppColors.darkCard : const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: isDark ? AppColors.darkBorder : AppColors.lightBorder,
              ),
            ),
            child: Column(
              children: [
                Icon(
                  Icons.image_outlined,
                  size: 32,
                  color: isDark
                      ? AppColors.darkTextMuted
                      : AppColors.lightTextMuted,
                ),
                const SizedBox(height: 6),
                Text(
                  'No completion photos attached yet',
                  style: TextStyle(
                    fontSize: 12,
                    color: isDark
                        ? AppColors.darkTextSecondary
                        : AppColors.lightTextSecondary,
                  ),
                ),
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  onPressed: showImageSourceDialog,
                  icon: const Icon(Icons.camera_alt_outlined, size: 14),
                  label:
                      const Text('Add photo', style: TextStyle(fontSize: 12)),
                ),
              ],
            ),
          )
        else
          SizedBox(
            height: 105,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: widget.photos.length + 1,
              separatorBuilder: (_, __) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                if (index == widget.photos.length) {
                  return InkWell(
                    onTap: showImageSourceDialog,
                    borderRadius: BorderRadius.circular(8),
                    child: Container(
                      width: 100,
                      height: 100,
                      decoration: BoxDecoration(
                        color: isDark
                            ? AppColors.darkCard
                            : const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: isDark
                              ? AppColors.darkBorder
                              : AppColors.lightBorder,
                        ),
                      ),
                      child: const Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.add_a_photo_outlined,
                              color: AppColors.primary, size: 28),
                          SizedBox(height: 4),
                          Text('Add photo',
                              style: TextStyle(
                                  fontSize: 11, fontWeight: FontWeight.w600)),
                        ],
                      ),
                    ),
                  );
                }

                final photo = widget.photos[index];
                return PhotoThumbnail(
                  fileName: photo.name,
                  bytes: photo.bytes,
                  isUploading: photo.isUploading,
                  isUploaded: photo.isUploaded,
                  onRemove: () => widget.onRemovePhoto?.call(photo.id),
                  onRetry: () => _retryUpload(photo),
                );
              },
            ),
          ),
      ],
    );
  }
}
