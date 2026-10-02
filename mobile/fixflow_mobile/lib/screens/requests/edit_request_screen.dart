import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import '../../providers/request_provider.dart';
import '../../models/request_model.dart';

class EditRequestScreen extends StatefulWidget {
  const EditRequestScreen({super.key});

  @override
  State<EditRequestScreen> createState() => _EditRequestScreenState();
}

class _EditRequestScreenState extends State<EditRequestScreen> {
  final _formKey        = GlobalKey<FormState>();
  final _titleCtrl      = TextEditingController();
  final _descCtrl       = TextEditingController();

  String? _requestId;
  String? _selectedLocationId;
  String? _selectedBuilding;
  String? _selectedFloor;

  Uint8List? _pickedImageBytes;
  String? _pickedImageName;
  bool _submitted = false;

  // Backend RequestService.UploadAttachmentAsync rejects files above 5 MB.
  static const int _maxImageBytes = 5 * 1024 * 1024;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final String? id = ModalRoute.of(context)?.settings.arguments as String?;
      if (id != null) {
        _requestId = id;
        _loadData();
      }
    });
  }

  Future<void> _loadData() async {
    final provider = context.read<RequestProvider>();
    await provider.loadDropdowns();
    await provider.fetchDetail(_requestId!);
    
    final detail = provider.detail;
    if (detail != null) {
      setState(() {
        _titleCtrl.text = detail.title;
        _descCtrl.text = detail.description;
        _selectedLocationId = detail.locationId;

        final loc = provider.locations.cast<DropdownOption?>().firstWhere(
            (l) => l?.id == detail.locationId, orElse: () => null);
        if (loc != null) {
          _selectedBuilding = loc.building.isNotEmpty ? loc.building : null;
          _selectedFloor = loc.floor.isNotEmpty ? loc.floor : null;
        }
      });
    }
  }

  Future<void> _pickImage() async {
    try {
      final picker = ImagePicker();
      final picked =
          await picker.pickImage(source: ImageSource.gallery, imageQuality: 85);
      if (picked != null) {
        final bytes = await picked.readAsBytes();
        if (bytes.length > _maxImageBytes) {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Photo is too large. Maximum size is 5 MB.'),
                backgroundColor: Colors.red,
              ),
            );
          }
          return;
        }
        setState(() {
          _pickedImageBytes = bytes;
          _pickedImageName = picked.name;
        });
      }
    } catch (_) {
      // Picker cancelled or gallery unavailable — keep the previous selection.
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final provider = context.read<RequestProvider>();

    // Attempt edit (category is AI-owned and never edited here)
    final success = await provider.updateRequest(
      _requestId!,
      title:       _titleCtrl.text.trim(),
      description: _descCtrl.text.trim(),
      locationId:  _selectedLocationId!,
    );

    if (success) {
      // Upload only happens after the update succeeded — a photo failure
      // must not discard the edited request.
      bool uploadOk = true;
      if (_pickedImageBytes != null) {
        uploadOk = await provider.uploadAttachment(
          _requestId!,
          _pickedImageBytes!,
          _pickedImageName ?? 'photo.jpg',
        );
        if (!uploadOk) provider.clearError();
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(uploadOk
              ? '✅ Request updated!'
              : '✅ Request updated, but the photo could not be uploaded. Please try again later.'),
          backgroundColor: uploadOk ? Colors.green : Colors.orange,
          duration: uploadOk
              ? const Duration(seconds: 1)
              : const Duration(seconds: 4),
        ),
      );
      setState(() => _submitted = true);
      // Wait for banner, then go back to detail screen
      await Future.delayed(const Duration(seconds: 1));
      if (mounted) Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider   = context.watch<RequestProvider>();
    final locations  = provider.locations;
    final isLoading  = provider.isLoading;

    final buildings = locations.map((l) => l.building).where((b) => b.isNotEmpty).toSet().toList()
      ..sort();

    List<String> floors;
    if (_selectedBuilding != null) {
      floors = locations
          .where((l) => l.building == _selectedBuilding)
          .map((l) => l.floor)
          .where((f) => f.isNotEmpty)
          .toSet()
          .toList()
        ..sort();
    } else {
      floors = <String>[];
    }

    final List<DropdownOption> rooms = (_selectedBuilding != null && _selectedFloor != null)
        ? locations.where((l) => l.building == _selectedBuilding && l.floor == _selectedFloor).toList()
        : <DropdownOption>[];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Edit Request'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (provider.error != null)
                Container(
                  margin: const EdgeInsets.only(bottom: 12),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    border: Border.all(color: Colors.red.shade200),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(provider.error!,
                      style: TextStyle(color: Colors.red.shade800)),
                ),

              TextFormField(
                controller:   _titleCtrl,
                maxLength:    200,
                enabled:      !isLoading && !_submitted,
                decoration: const InputDecoration(
                  labelText:   'Title *',
                  hintText:    'e.g. Bathroom tap leaking in Unit 305',
                  border:      OutlineInputBorder(),
                  prefixIcon:  Icon(Icons.title),
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Title is required.';
                  if (v.trim().length > 200) return 'Max 200 characters.';
                  return null;
                },
              ),
              const SizedBox(height: 12),

              TextFormField(
                controller: _descCtrl,
                maxLines:   4,
                enabled:    !isLoading && !_submitted,
                decoration: const InputDecoration(
                  labelText:    'Description *',
                  hintText:     'Describe the issue in detail (at least 20 characters)…',
                  border:       OutlineInputBorder(),
                  alignLabelWithHint: true,
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Description is required.';
                  if (v.trim().length < 20) return 'At least 20 characters required.';
                  return null;
                },
              ),
              const SizedBox(height: 12),

              DropdownButtonFormField<String>(
                key: ValueKey('building-$_selectedBuilding'),
                initialValue: _selectedBuilding,
                decoration: const InputDecoration(
                  labelText: 'Building *',
                  border:    OutlineInputBorder(),
                  prefixIcon: Icon(Icons.business),
                ),
                items: buildings.map((b) => DropdownMenuItem(value: b, child: Text(b))).toList(),
                onChanged: isLoading || _submitted
                    ? null
                    : (v) => setState(() {
                          _selectedBuilding = v;
                          _selectedFloor = null;
                          _selectedLocationId = null;
                        }),
                validator: (v) => v == null || v.isEmpty ? 'Please select a building.' : null,
              ),
              const SizedBox(height: 12),

              DropdownButtonFormField<String>(
                key: ValueKey('floor-$_selectedFloor'),
                initialValue: _selectedFloor,
                decoration: const InputDecoration(
                  labelText: 'Floor *',
                  border:    OutlineInputBorder(),
                  prefixIcon: Icon(Icons.layers),
                ),
                items: floors.map((f) => DropdownMenuItem(value: f, child: Text(f))).toList(),
                onChanged: (_selectedBuilding == null || isLoading || _submitted)
                    ? null
                    : (v) => setState(() {
                          _selectedFloor = v;
                          _selectedLocationId = null;
                        }),
                validator: (v) => v == null || v.isEmpty ? 'Please select a floor.' : null,
              ),
              const SizedBox(height: 12),

              DropdownButtonFormField<String>(
                key: ValueKey('location-$_selectedLocationId'),
                initialValue: _selectedLocationId,
                decoration: const InputDecoration(
                  labelText: 'Room / Unit *',
                  border:    OutlineInputBorder(),
                  prefixIcon: Icon(Icons.meeting_room),
                ),
                items: rooms.map((r) {
                  final label = r.room.isNotEmpty ? r.room : r.name;
                  return DropdownMenuItem<String>(value: r.id, child: Text(label));
                }).toList(),
                onChanged: (_selectedFloor == null || isLoading || _submitted)
                    ? null
                    : (v) => setState(() => _selectedLocationId = v),
                validator: (v) => v == null || v.isEmpty ? 'Please select a room.' : null,
              ),
              const SizedBox(height: 12),

              // Category is intentionally absent — it is AI-determined via
              // classification, never chosen by the requester (same as React).

              const SizedBox(height: 12),

              InkWell(
                onTap: (isLoading || _submitted) ? null : _pickImage,
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.blue.shade50,
                    border: Border.all(color: Colors.blue.shade200, width: 2),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.camera_alt, color: Colors.blue.shade700),
                      const SizedBox(width: 8),
                      Text('Attach Photo (Optional)',
                          style: TextStyle(
                            color: Colors.blue.shade700,
                            fontWeight: FontWeight.bold,
                          )),
                    ],
                  ),
                ),
              ),
              
              if (_pickedImageBytes != null)
                Padding(
                  padding: const EdgeInsets.only(top: 16),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: Image.memory(_pickedImageBytes!, height: 80, width: 80, fit: BoxFit.cover),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          _pickedImageName ?? 'photo.jpg',
                          style: TextStyle(color: Colors.grey.shade600, fontSize: 12),
                        ),
                      ),
                    ],
                  ),
                ),

              const SizedBox(height: 24),

              if (_submitted)
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.green.shade50,
                    border: Border.all(color: Colors.green.shade200),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text('✅ Request updated!',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Colors.green.shade800)),
                )
              else
                SizedBox(
                  height: 50,
                  child: ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF2563EB),
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: isLoading ? null : _submit,
                    child: isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                          )
                        : const Text('Save Changes', style: TextStyle(fontSize: 16)),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
