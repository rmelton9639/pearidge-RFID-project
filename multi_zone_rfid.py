#!/usr/bin/env python3
"""
Dog Grooming Multi-Zone RFID Tracker
Cycles through 4 antenna zones to track dogs through grooming process

Zone 1: Bathing Room
Zone 2: Drying Kennels
Zone 3: Grooming Room 1
Zone 4: Grooming Room 2
"""

import serial
import socket
import time
import json
import os
from datetime import datetime

# Zone Configuration
ZONES = {
    1: "Bathing Room",
    2: "Drying Kennels",
    3: "Grooming Room 1",
    4: "Grooming Room 2"
}

# Network Configuration
TCP_HOST = os.getenv('RFID_HOST', '0.0.0.0')
TCP_PORT = int(os.getenv('RFID_PORT', '5002'))
DEVICE_ID = os.getenv('RFID_DEVICE_ID', 'GROOMING_RFID_4ZONE')

# UART Configuration
UART_PORT = os.getenv('RFID_UART_PORT', '/dev/ttyAMA0')
UART_BAUD = 115200

# Timing Configuration
ZONE_READ_TIME = 0.5  # How long to read each zone (seconds)
CYCLE_DELAY = 0.1     # Delay between zones (seconds)

def set_antenna(ser, antenna_num):
    """Set active antenna (1-4)"""
    antenna_commands = {
        1: bytes.fromhex("FF 02 61 00 01 9C 7E"),
        2: bytes.fromhex("FF 02 61 00 02 BD BE"),
        3: bytes.fromhex("FF 02 61 00 03 DD 7F"),
        4: bytes.fromhex("FF 02 61 00 04 3C FF")
    }
    
    if antenna_num in antenna_commands:
        ser.write(antenna_commands[antenna_num])
        time.sleep(0.05)
        return True
    return False

def parse_epc(data):
    """Parse EPC tag from M6E response"""
    try:
        if len(data) < 10 or data[0] != 0xFF:
            return None
        
        status = data[2]
        if status != 0x00:  # Not success
            return None
        
        # Look for EPC data (try multiple offsets)
        for start_offset in range(9, min(len(data) - 6, 20)):
            if start_offset + 12 <= len(data):
                epc_data = data[start_offset:start_offset + 12]
                if not all(b == 0 for b in epc_data) and not all(b == 0xFF for b in epc_data):
                    return ''.join([f'{b:02X}' for b in epc_data])
        
        return None
    except:
        return None

def read_from_zone(ser, zone_num):
    """Read tags from specific zone"""
    # Switch to antenna
    set_antenna(ser, zone_num)
    time.sleep(0.05)
    
    # Send read command
    read_cmd = bytes.fromhex("FF 04 22 00 00 01 F4 FB A6")  # 500ms read
    ser.write(read_cmd)
    time.sleep(ZONE_READ_TIME)
    
    # Check for response
    if ser.in_waiting > 0:
        response = ser.read(ser.in_waiting)
        epc = parse_epc(response)
        
        if epc:
            return {
                'zone': zone_num,
                'zone_name': ZONES[zone_num],
                'epc': epc,
                'timestamp': datetime.now().isoformat()
            }
    
    return None

def send_to_pos(client_sockets, tag_info):
    """Send tag read to POS clients"""
    message = {
        'timestamp': tag_info['timestamp'],
        'device_id': DEVICE_ID,
        'rfid_tag': tag_info['epc'],
        'zone': tag_info['zone'],
        'zone_name': tag_info['zone_name'],
        'location': tag_info['zone_name']
    }
    
    json_message = json.dumps(message) + '\n'
    message_bytes = json_message.encode('utf-8')
    
    disconnected = []
    for client in client_sockets:
        try:
            client.sendall(message_bytes)
        except:
            disconnected.append(client)
    
    # Remove disconnected clients
    for client in disconnected:
        client_sockets.remove(client)
        try:
            client.close()
        except:
            pass

def accept_client(server_socket, client_sockets):
    """Accept new client connection (non-blocking)"""
    try:
        server_socket.settimeout(0.1)
        client_socket, client_address = server_socket.accept()
        client_sockets.append(client_socket)
        print(f"✓ POS client connected from {client_address}")
    except socket.timeout:
        pass
    except:
        pass

def main():
    """Main multi-zone tracking loop"""
    print("=" * 70)
    print("Dog Grooming RFID Tracker - 4 Zone System")
    print("=" * 70)
    print()
    print("Zones:")
    for zone_num, zone_name in ZONES.items():
        print(f"  Zone {zone_num}: {zone_name}")
    print()
    print(f"TCP Server: {TCP_HOST}:{TCP_PORT}")
    print(f"Device ID: {DEVICE_ID}")
    print("=" * 70)
    print()
    
    # Open serial port
    try:
        ser = serial.Serial(UART_PORT, UART_BAUD, timeout=1)
        print(f"✓ Connected to M6E on {UART_PORT}")
    except Exception as e:
        print(f"✗ Failed to open {UART_PORT}: {e}")
        return
    
    # Setup TCP server
    try:
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_socket.bind((TCP_HOST, TCP_PORT))
        server_socket.listen(5)
        server_socket.setblocking(False)
        print(f"✓ TCP server listening on {TCP_HOST}:{TCP_PORT}")
    except Exception as e:
        print(f"✗ Failed to start TCP server: {e}")
        ser.close()
        return
    
    client_sockets = []
    
    # Initialize M6E
    print("\nInitializing M6E Nano...")
    
    # Set region
    region_cmd = bytes.fromhex("FF 0A 04 03 00 04 01 02 06 60")
    ser.write(region_cmd)
    time.sleep(0.2)
    
    # Set power to 27 dBm (good for 10.5 dBi antennas)
    power_cmd = bytes.fromhex("FF 07 04 03 00 05 02 02 0A 8C AF 49")
    ser.write(power_cmd)
    time.sleep(0.2)
    
    print("✓ M6E initialized")
    print()
    print("Tracking dogs through grooming process...")
    print("Press Ctrl+C to stop")
    print("-" * 70)
    
    last_tags = {}  # Track last seen tags per zone to avoid duplicates
    cycle_count = 0
    
    try:
        while True:
            # Accept new POS connections
            accept_client(server_socket, client_sockets)
            
            # Cycle through all zones
            for zone in range(1, 5):
                tag_info = read_from_zone(ser, zone)
                
                if tag_info:
                    epc = tag_info['epc']
                    zone_num = tag_info['zone']
                    
                    # Check if this is a new read (not a duplicate from last cycle)
                    last_key = f"{zone_num}:{epc}"
                    current_time = time.time()
                    
                    if last_key not in last_tags or (current_time - last_tags[last_key]) > 5.0:
                        # New tag or hasn't been seen in 5 seconds
                        last_tags[last_key] = current_time
                        
                        print(f"🐕 Dog detected in {tag_info['zone_name']}")
                        print(f"   Tag: {epc}")
                        print(f"   Time: {tag_info['timestamp']}")
                        
                        if client_sockets:
                            send_to_pos(client_sockets, tag_info)
                            print(f"   ✓ Sent to {len(client_sockets)} POS client(s)")
                        else:
                            print(f"   ⚠ No POS clients connected")
                        
                        print()
                
                time.sleep(CYCLE_DELAY)
            
            cycle_count += 1
            if cycle_count % 20 == 0:  # Status every 20 cycles
                print(f"[Status] Completed {cycle_count} scan cycles, {len(client_sockets)} POS clients connected")
    
    except KeyboardInterrupt:
        print("\n\nShutting down...")
    
    finally:
        # Cleanup
        for client in client_sockets:
            try:
                client.close()
            except:
                pass
        
        try:
            server_socket.close()
        except:
            pass
        
        ser.close()
        print("✓ Stopped cleanly")

if __name__ == "__main__":
    main()
