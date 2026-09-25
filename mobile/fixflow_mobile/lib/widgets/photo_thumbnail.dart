import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../core/theme/app_colors.dart';

class PhotoThumbnail extends StatelessWidget {
  final String fileName;
  final Uint8List? bytes;
  final String? url;
  final bool isUploading;
  final bool isUploaded;
  final VoidCallback? onRemove;
  final VoidCallback? onRetry;

  const PhotoThumbnail({
    super.key,
    required this.fileName,
    this.bytes,
    this.url,
    this.isUploading = false,
    this.isUploaded = false,
    this.onRemove,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      width: 100,
      height: 100,
      decoration: BoxDecoration(
        color: isDark ? AppColors.darkCard : const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: isUploaded
              ? AppColors.completed
              : isDark
                  ? AppColors.darkBorder
                  : AppColors.lightBorder,
          width: isUploaded ? 1.5 : 1.0,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        fit: StackFit.expand,
        children: [
          // Image or Placeholder
          if (bytes != null)
            Image.memory(
              bytes!,
              fit: BoxFit.cover,
            )
          else
            Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.image, size: 28, color: AppColors.primary),
                  const SizedBox(height: 4),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    child: Text(
                      fileName,
                      style: const TextStyle(fontSize: 9),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),

          // Uploading overlay
          if (isUploading)
            Container(
              color: Colors.black45,
              child: const Center(
                child: SizedBox(
                  width: 24,
                  height: 24,
                  child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                ),
              ),
            ),

          // Uploaded Success badge
          if (isUploaded)
            Positioned(
              bottom: 4,
              right: 4,
              child: Container(
                padding: const EdgeInsets.all(2),
                decoration: const BoxDecoration(
                  color: AppColors.completed,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.check, size: 12, color: Colors.white),
              ),
            ),

          // Remove Button
          if (onRemove != null && !isUploading)
            Positioned(
              top: 4,
              right: 4,
              child: GestureDetector(
                onTap: onRemove,
                child: Container(
                  padding: const EdgeInsets.all(3),
                  decoration: const BoxDecoration(
                    color: Colors.black54,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.close, size: 12, color: Colors.white),
                ),
              ),
            ),

          // Retry button if error
          if (onRetry != null && !isUploading && !isUploaded)
            Positioned(
              bottom: 4,
              left: 4,
              child: GestureDetector(
                onTap: onRetry,
                child: Container(
                  padding: const EdgeInsets.all(3),
                  decoration: const BoxDecoration(
                    color: AppColors.critical,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.refresh, size: 12, color: Colors.white),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
