import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter/material.dart';
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
  final Function(PhotoItem item)? onAddPhoto;
  final Function(String id)? onRemovePhoto;
  final Future<String?> Function(PhotoItem item)? onUploadPhoto;

  const PhotoUpload({
    super.key,
    required this.photos,
    this.onAddPhoto,
    this.onRemovePhoto,
    this.onUploadPhoto,
  });

  @override
  State<PhotoUpload> createState() => _PhotoUploadState();
}

class _PhotoUploadState extends State<PhotoUpload> {
  // Minimal valid 1x1 PNG bytes for simulation / device fallback
  static final Uint8List _samplePng = base64Decode(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
  );

  void _showImageSourceDialog() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ListTile(
                  leading: const CircleAvatar(
                    backgroundColor: AppColors.primaryAccent,
                    child: Icon(Icons.camera_alt, color: AppColors.primary),
                  ),
                  title: const Text('Take Photo (Camera)', style: TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: const Text('Capture repair/after photo with device camera'),
                  onTap: () {
                    Navigator.pop(context);
                    _handleCapturePhoto('camera');
                  },
                ),
                const Divider(),
                ListTile(
                  leading: const CircleAvatar(
                    backgroundColor: AppColors.primaryAccent,
                    child: Icon(Icons.photo_library, color: AppColors.primary),
                  ),
                  title: const Text('Choose from Gallery', style: TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: const Text('Select existing evidence photo'),
                  onTap: () {
                    Navigator.pop(context);
                    _handleCapturePhoto('gallery');
                  },
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _handleCapturePhoto(String source) async {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final fileName = 'evidence_${source}_$timestamp.jpg';

    final newItem = PhotoItem(
      id: timestamp.toString(),
      name: fileName,
      bytes: _samplePng,
      isUploading: false,
      isUploaded: false,
    );

    widget.onAddPhoto?.call(newItem);

    if (widget.onUploadPhoto != null) {
      setState(() => newItem.isUploading = true);
      final key = await widget.onUploadPhoto!(newItem);
      if (mounted) {
        setState(() {
          newItem.isUploading = false;
          if (key != null) {
            newItem.isUploaded = true;
            newItem.fileKey = key;
          }
        });
      }
    }
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
              'Evidence & Work Photos (${widget.photos.length})',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
              ),
            ),
            TextButton.icon(
              onPressed: _showImageSourceDialog,
              icon: const Icon(Icons.add_a_photo, size: 16),
              label: const Text('Add Photo', style: TextStyle(fontSize: 12)),
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
                style: BorderStyle.solid,
              ),
            ),
            child: Column(
              children: [
                Icon(
                  Icons.camera_enhance_outlined,
                  size: 32,
                  color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                ),
                const SizedBox(height: 6),
                Text(
                  'No completion photos attached yet',
                  style: TextStyle(
                    fontSize: 12,
                    color: isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary,
                  ),
                ),
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  onPressed: _showImageSourceDialog,
                  icon: const Icon(Icons.camera_alt, size: 14),
                  label: const Text('Capture After Photo', style: TextStyle(fontSize: 12)),
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
                    onTap: _showImageSourceDialog,
                    borderRadius: BorderRadius.circular(8),
                    child: Container(
                      width: 100,
                      height: 100,
                      decoration: BoxDecoration(
                        color: isDark ? AppColors.darkCard : const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: isDark ? AppColors.darkBorder : AppColors.lightBorder,
                        ),
                      ),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: const [
                          Icon(Icons.add, color: AppColors.primary, size: 28),
                          SizedBox(height: 4),
                          Text('Add More', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600)),
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
                  onRetry: () async {
                    if (widget.onUploadPhoto != null) {
                      setState(() => photo.isUploading = true);
                      final key = await widget.onUploadPhoto!(photo);
                      if (mounted) {
                        setState(() {
                          photo.isUploading = false;
                          if (key != null) {
                            photo.isUploaded = true;
                            photo.fileKey = key;
                          }
                        });
                      }
                    }
                  },
                );
              },
            ),
          ),
      ],
    );
  }
}
