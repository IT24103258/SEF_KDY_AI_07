import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import '../../providers/request_provider.dart';
import '../../core/routes/app_router.dart';
import '../../models/request_model.dart';

/// Submit Maintenance Request screen — /submit-request  (Requester only).
///
/// Loads location and category dropdowns from the API on first open.
/// Validates title (≥1 char, ≤200), description (≥20 chars), location (required)
/// client-side before hitting the backend — mirrors the FluentValidation rules.
class SubmitRequestScreen extends StatefulWidget {
  const SubmitRequestScreen({super.key});

  @override
  State<SubmitRequestScreen> createState() => _SubmitRequestScreenState();
}

class _SubmitRequestScreenState extends State<SubmitRequestScreen> {
  final _formKey        = GlobalKey<FormState>();
  final _titleCtrl      = TextEditingController();
  final _descCtrl       = TextEditingController();

  String? _selectedLocationId;
  String? _selectedBuilding;
  String? _selectedFloor;
  // String? _selectedCategoryId; --- IGNORE --- as this is now AI-determined and not set by the user

  Uint8List? _pickedImageBytes;

  bool _submitted = false;

  @override
  void initState() {
    super.initState();
    // Load dropdowns after the first frame so the provider is available
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<RequestProvider>().loadDropdowns();
    });
  }

  Future<void> _pickImage() async {
    final picker = ImagePicker();
    final pickedFile = await picker.pickImage(source: ImageSource.gallery);
    if (pickedFile != null) {
      final bytes = await pickedFile.readAsBytes();
      setState(() => _pickedImageBytes = bytes);
    }
  }

  @override
  void dispose() {
    _titleCtrl.dispose();
    _descCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final provider = context.read<RequestProvider>();
    final success  = await provider.createRequest(
      title:       _titleCtrl.text.trim(),
      description: _descCtrl.text.trim(),
      locationId:  _selectedLocationId!,
    );

    if (!mounted) return;

    if (success) {
      setState(() => _submitted = true);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Request submitted successfully!'),
          backgroundColor: Colors.green,
        ),
      );
      await Future.delayed(const Duration(milliseconds: 800));
      if (mounted) Navigator.pushReplacementNamed(context, AppRouter.myRequests);
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
        title: const Text('Submit Maintenance Request'),
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
              // ── Error banner ──────────────────────────────────────────────
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

              // ── Title ─────────────────────────────────────────────────────
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

              // ── Description ───────────────────────────────────────────────
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

              // ── Location dropdown (Cascading) ─────────────────────────────
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
                key: ValueKey(_selectedLocationId),
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

              const SizedBox(height: 12),

              // ── Image Picker (optional) ───────────────────────────────────
              const Text('Photo (optional)', style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              if (_pickedImageBytes != null) ...[
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.memory(_pickedImageBytes!, height: 150, width: double.infinity, fit: BoxFit.cover),
                ),
                const SizedBox(height: 8),
              ],
              OutlinedButton.icon(
                onPressed: isLoading || _submitted ? null : _pickImage,
                icon: const Icon(Icons.camera_alt),
                label: Text(_pickedImageBytes == null ? 'Attach Photo' : 'Change Photo'),
              ),
              const Padding(
                padding: EdgeInsets.only(top: 4, bottom: 24),
                child: Text(
                  'Note: Cloudinary upload implementation is a follow-up.',
                  style: TextStyle(fontSize: 12, fontStyle: FontStyle.italic, color: Colors.grey),
                ),
              ),

              // ── Submit button ─────────────────────────────────────────────
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  onPressed: isLoading || _submitted ? null : _submit,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF2563EB),
                    foregroundColor: Colors.white,
                  ),
                  child: isLoading
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Submit Request',
                          style: TextStyle(fontSize: 16)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
